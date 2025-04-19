using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using MCRGame.UI;

namespace MCRGame.Net
{
    public class RoomWS : MonoBehaviour
    {
        public static RoomWS Instance { get; private set; }

        public int roomNumber = 1;
        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellation;

        // 연결 완료 시 호출할 콜백
        public Action OnWebSocketConnected;

        private void Awake()
        {
            // 싱글턴 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 연결 완료 시 초기 데이터 로드 핸들러 등록
            OnWebSocketConnected += HandleOnConnected;
        }

        async void Start()
        {
            // RoomDataManager에 저장된 RoomId(문자열)를 int로 변환
            if (RoomDataManager.Instance != null &&
                int.TryParse(RoomDataManager.Instance.RoomId, out int parsed))
            {
                roomNumber = parsed;
                Debug.Log($"[RoomWS] roomNumber ← {roomNumber}");
            }

            cancellation = new CancellationTokenSource();
            await Connect();
        }

        private void HandleOnConnected()
        {
            Debug.Log("[RoomWS] OnWebSocketConnected → HTTP로 초기 유저 리스트 요청");

            var api = RoomApiManager.Instance;
            if (api == null)
            {
                Debug.LogWarning("[RoomWS] RoomApiManager를 찾을 수 없습니다.");
                return;
            }

            StartCoroutine(
                api.FetchRoomUsers(
                    roomNumber.ToString(),
                    (resp) =>
                    {
                        Debug.Log($"호스트: {resp.host_uid}");
                        foreach (var u in resp.users)
                            Debug.Log($"slot {u.slot_index}: {u.nickname}({u.uid}) ready={u.isReady}");
                    },
                    (err) => Debug.LogError("FetchRoomUsers failed: " + err)
                )
            );

        }

        private async Task Connect()
        {
            webSocket = new ClientWebSocket();
            string token = PlayerDataManager.Instance?.AccessToken ?? "";
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[RoomWS] 토큰이 없습니다.");
                return;
            }
            webSocket.Options.SetRequestHeader("authorization", token);

            var uri = new Uri(CoreServerConfig.GetWebSocketUrl($"/ws/room/{roomNumber}"));
            Debug.Log("[RoomWS] Connecting to " + uri);
            try
            {
                await webSocket.ConnectAsync(uri, cancellation.Token);
                Debug.Log("[RoomWS] WebSocket 연결 성공");
                OnWebSocketConnected?.Invoke();
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoomWS] Connect 에러: " + ex.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(buffer, cancellation.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellation.Token);
                        Debug.Log("[RoomWS] 서버가 연결을 닫음");
                        break;
                    }

                    string msg = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    Debug.Log("[RoomWS] 수신: " + msg);
                    ProcessMessage(msg);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[RoomWS] ReceiveLoop 에러: " + ex.Message);
                    break;
                }
            }
        }


        private void ProcessMessage(string message)
        {
            WSMessage resp;
            try
            {
                resp = JsonConvert.DeserializeObject<WSMessage>(message);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RoomWS] JSON 파싱 실패: " + e.Message);
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
                        var d = JsonConvert.DeserializeObject<WSUserReadyData>(resp.Data.ToString());
                        var rm = FindAnyObjectByType<RoomManager>();
                        rm?.UpdatePlayerReadyState(d.UserUid, d.IsReady);
                        break;
                    }

                case WSActionType.USER_JOINED:
                    {
                        var d = JsonConvert.DeserializeObject<WSUserJoinedData>(resp.Data.ToString());
                        var rdm = RoomDataManager.Instance;
                        if (rdm != null)
                            rdm.AddOrUpdateUser(new RoomUserData
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
                            // 호스트 배치
                            if (!string.IsNullOrEmpty(list.HostUid))
                            {
                                // HostUser와 slot_index는 RoomDataManager 에 이미 설정되어 있다고 가정
                            }
                            // 전체 유저 배치
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
                        if (rm2 != null)
                        {
                            rm2.OnHostChanged(list.HostUid);
                            rm2.UpdatePlayerUI();
                        }
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
        public async Task SendLeaveAsync()
        {
            var msg = new WSMessage
            {
                Status = "success",
                Action = WSActionType.LEAVE,
                Data = new { },
                Error = null,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            await SendJson(msg);
            Debug.Log("[RoomWS] LEAVE 액션 전송 완료");
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
            await SendJson(pong);
        }

        private async Task SendJson(object obj)
        {
            if (webSocket?.State != WebSocketState.Open) return;
            string json = JsonConvert.SerializeObject(obj);
            var buf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            try { await webSocket.SendAsync(buf, WebSocketMessageType.Text, true, cancellation.Token); }
            catch (Exception e) { Debug.LogError("[RoomWS] Send 에러: " + e.Message); }
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
            await SendJson(msg);
        }

        private void OnDestroy()
        {
            cancellation?.Cancel();
            webSocket?.Abort();
            webSocket = null;
            Instance = null;
        }
    }
}
