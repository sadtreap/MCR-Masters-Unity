using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCRGame.Net
{
    public class RoomWS : MonoBehaviour
    {
        public static RoomWS Instance { get; private set; }
        public int roomNumber = 1;

        private WebSocket websocket;
        public event Action OnWebSocketConnected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            OnWebSocketConnected += HandleOnConnected;
        }

        private async void Start()
        {
            // 1) 방 번호 가져오기
            if (RoomDataManager.Instance != null &&
                int.TryParse(RoomDataManager.Instance.RoomId, out int parsed))
            {
                roomNumber = parsed;
            }

            // 2) WebSocket URL + 토큰
            string baseUrl = CoreServerConfig.GetWebSocketUrl("/ws/room/" + roomNumber);
            string token = PlayerDataManager.Instance?.AccessToken ?? "";
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[RoomWS] AccessToken이 없습니다.");
                return;
            }

            // 3) 쿼리스트링에 토큰 전달
            string url = $"{baseUrl}?authorization={Uri.EscapeDataString(token)}";
            Debug.Log($"[RoomWS] WebSocket URL: {url}");
            // 4) WebSocket 생성
            websocket = new WebSocket(url);

            // 5) 이벤트 핸들러 등록
            websocket.OnOpen += () =>
            {
                Debug.Log("[RoomWS] WebSocket 연결 성공");
                OnWebSocketConnected?.Invoke();
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError("[RoomWS] WebSocket Error: " + e);
            };

            websocket.OnClose += (e) =>
            {
                Debug.LogWarning($"[RoomWS] WebSocket closed: {e}");
            };

            websocket.OnMessage += (bytes) =>
            {
                var msg = Encoding.UTF8.GetString(bytes);
                Debug.Log("[RoomWS] 수신: " + msg);
                ProcessMessage(msg);
            };

            // 6) 연결 시도
            Debug.Log("[RoomWS] Connecting to: " + url);
            await websocket.Connect();
        }

        // NativeWebSocket은 매 프레임마다 DispatchMessageQueue 호출이 필요합니다
        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif
        }

        private void HandleOnConnected()
        {
            Debug.Log("[RoomWS] OnWebSocketConnected → HTTP로 초기 유저 리스트 요청");
            StartCoroutine(RoomApiManager.Instance.FetchRoomUsers(
                roomNumber.ToString(),
                resp =>
                {
                    Debug.Log($"호스트: {resp.host_uid}");
                    foreach (var u in resp.users)
                        Debug.Log($"slot {u.slot_index}: {u.nickname}({u.uid}) ready={u.isReady}");
                },
                err => Debug.LogError("FetchRoomUsers failed: " + err)
            ));
        }

        private void ProcessMessage(string message)
        {
            WSMessage resp;
            try
            {
                resp = JsonConvert.DeserializeObject<WSMessage>(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoomWS] JSON 파싱 실패: " + ex.Message);
                return;
            }
            if (resp == null || resp.Data == null)
            {
                Debug.LogWarning("[RoomWS] Action 또는 Data가 없습니다.");
                return;
            }

            switch (resp.Action)
            {
                case WSActionType.PING:
                    SendPong();
                    break;
                case WSActionType.PONG:
                    Debug.Log("[RoomWS] PONG 수신");
                    break;
                case WSActionType.USER_READY_CHANGED:
                    {
                        var jobj = resp.Data as JObject;
                        if (jobj == null)
                        {
                            Debug.LogWarning("[RoomWS] USER_READY_CHANGED: Data가 JObject가 아닙니다.");
                            break;
                        }
                        string uid = jobj["user_uid"]?.Value<string>() ?? "";
                        bool readyStatus = jobj["is_ready"]?.Value<bool>() ?? false;
                        Debug.Log($"[RoomWS] USER_READY_CHANGED 파싱: uid={uid}, is_ready={readyStatus}");
                        FindAnyObjectByType<RoomManager>()?.UpdatePlayerReadyState(uid, readyStatus);
                        break;
                    }
                case WSActionType.USER_JOINED:
                    {
                        var d = JsonConvert.DeserializeObject<WSUserJoinedData>(resp.Data.ToString());
                        RoomDataManager.Instance?.AddOrUpdateUser(new RoomUserData
                        {
                            uid = d.UserUid,
                            nickname = d.Nickname,
                            isReady = d.IsReady,
                            slot_index = d.SlotIndex
                        });
                        FindAnyObjectByType<RoomManager>()?.UpdatePlayerUI();
                        break;
                    }
                case WSActionType.USER_LEFT:
                    {
                        var d = JsonConvert.DeserializeObject<WSUserLeftData>(resp.Data.ToString());
                        Debug.Log($"[RoomWS] USER_LEFT: {d.UserUid}");
                        break;
                    }
                case WSActionType.USER_LIST:
                    {
                        var list = JsonConvert.DeserializeObject<WSUserListData>(resp.Data.ToString());
                        var rdm = RoomDataManager.Instance;
                        if (rdm != null)
                        {
                            rdm.Players = new RoomUserData[4];
                            foreach (var u in list.Users)
                                rdm.Players[u.SlotIndex] = new RoomUserData
                                {
                                    uid = u.UserUid,
                                    nickname = u.Nickname,
                                    isReady = u.IsReady,
                                    slot_index = u.SlotIndex
                                };
                        }
                        var rm2 = FindAnyObjectByType<RoomManager>();
                        rm2?.OnHostChanged(list.HostUid);
                        rm2?.UpdatePlayerUI();
                        break;
                    }
                case WSActionType.GAME_STARTED:
                    {
                        var d = JsonConvert.DeserializeObject<WSGameStartedData>(resp.Data.ToString());
                        GameServerConfig.UpdateWebSocketConfig(d.GameUrl);
                        if (GameWS.Instance == null)
                            new GameObject("GameWS").AddComponent<GameWS>();
                        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
                        break;
                    }
                default:
                    Debug.LogWarning("[RoomWS] Unknown action: " + resp.Action);
                    break;
            }
        }

        public async void SendLeave()
        {
            var msg = new WSMessage
            {
                Status = "success",
                Action = WSActionType.LEAVE,
                Data = new { },
                Error = null,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            string json = JsonConvert.SerializeObject(msg);
            await websocket.SendText(json);
            Debug.Log("[RoomWS] LEAVE 전송 완료");
        }

        private async void SendPong()
        {
            var pong = new WSMessage
            {
                Status = "success",
                Action = WSActionType.PONG,
                Data = new { message = "pong" },
                Error = null,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            string json = JsonConvert.SerializeObject(pong);
            await websocket.SendText(json);
        }

        public async void SendReadyStatus(bool isReady)
        {
            var msg = new WSMessage
            {
                Status = "success",
                Action = WSActionType.READY,
                Data = new { is_ready = isReady },
                Error = null,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            string json = JsonConvert.SerializeObject(msg);
            await websocket.SendText(json);
            Debug.Log("[RoomWS] READY 전송 완료");
        }

        private async void OnDestroy()
        {
            if (websocket != null)
            {
                await websocket.Close();
                websocket = null;
            }
            Instance = null;
        }
    }
}
