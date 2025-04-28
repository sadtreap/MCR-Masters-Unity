using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using MCRGame.UI;
using MCRGame.Game;
using System.Collections;
using UnityEngine.Networking;

namespace MCRGame.Net
{
    public class GameWS : MonoBehaviour
    {
        public static GameWS Instance { get; private set; }
        private WebSocket websocket;

        // 재접속 관련
        private bool manualClose = false;
        private bool isReconnecting = false;
        private const int RECONNECT_DELAY = 5; // 초 단위

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            // 유저 정보가 비어있으면 먼저 받아오고, 그 후 WebSocket 연결
            StartCoroutine(EnsureUserDataThenConnect());
        }

        private IEnumerator EnsureUserDataThenConnect()
        {
            var pdm = PlayerDataManager.Instance;
            if (string.IsNullOrEmpty(pdm.Uid) || string.IsNullOrEmpty(pdm.Nickname))
            {
                yield return StartCoroutine(FetchUserInfoCoroutine());
            }
            Connect();
        }

        private IEnumerator FetchUserInfoCoroutine()
        {
            var getUserInfoUrl = CoreServerConfig.GetHttpUrl("/user/me");
            Debug.Log("[GameWS] ▶ Fetching user info before WS connect");
            using var www = UnityWebRequest.Get(getUserInfoUrl);
            www.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
            www.certificateHandler = new BypassCertificateHandler();
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[GameWS] ✔ User info fetched: " + www.downloadHandler.text);
                var userData = JsonConvert.DeserializeObject<UserMeResponse>(www.downloadHandler.text);
                PlayerDataManager.Instance.SetUserData(userData.uid, userData.nickname, userData.email);
            }
            else
            {
                Debug.LogError("[GameWS] ❌ Failed to fetch user info: " + www.error);
            }
        }
        private async void Connect()
        {
            manualClose = false;
            isReconnecting = false;

            // 1) URL 준비
            string baseUrl = GameServerConfig.GetWebSocketUrl();
            var pdm = PlayerDataManager.Instance;
            string uid = pdm?.Uid ?? "";
            string nick = pdm?.Nickname ?? "";
            string token = pdm?.AccessToken ?? "";

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[GameWS] AccessToken이 없습니다.");
                return;
            }

            string url = $"{baseUrl}?user_id={Uri.EscapeDataString(uid)}" +
                        $"&nickname={Uri.EscapeDataString(nick)}";

            // 2) NativeWebSocket 인스턴스 생성
            websocket = new WebSocket(url);

            websocket.OnOpen += () =>
            {
                Debug.Log("[GameWS] WebSocket connected!");
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError("[GameWS] WebSocket Error: " + e);
                // 에러 발생 후 연결이 끊길 가능성이 있으므로 재접속 시도
                TryReconnect();
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log($"[GameWS] WebSocket Closed: {e}");
                // 사용자가 직접 Close한 게 아니라면 재접속
                TryReconnect();
            };

            websocket.OnMessage += (bytes) =>
            {
                string msg = Encoding.UTF8.GetString(bytes);
                Debug.Log("[GameWS] Received: " + msg);
                try
                {
                    var wsMessage = JsonConvert.DeserializeObject<GameWSMessage>(msg);
                    if (wsMessage != null && GameMessageMediator.Instance != null)
                        GameMessageMediator.Instance.EnqueueMessage(wsMessage);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[GameWS] JSON deserialize error: " + ex);
                }
            };

            Debug.Log("[GameWS] Connecting to: " + url);
            await websocket.Connect();
        }

        private void TryReconnect()
        {
            if (manualClose || isReconnecting)
                return;

            isReconnecting = true;
            StartCoroutine(ReconnectCoroutine());
        }

        private IEnumerator ReconnectCoroutine()
        {
            Debug.Log($"[GameWS] 연결이 끊어졌습니다. {RECONNECT_DELAY}초 후 재접속 시도...");
            yield return new WaitForSeconds(RECONNECT_DELAY);
            Debug.Log("[GameWS] 재접속 시도...");
            Connect();
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif
        }

        public async void SendGameEvent(GameWSActionType action, object payload)
        {
            if (websocket == null || websocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("[GameWS] WebSocket is not open; cannot send.");
                return;
            }

            var messageObj = new
            {
                @event = action,
                data = payload
            };

            string json = JsonConvert.SerializeObject(messageObj);
            await websocket.SendText(json);
            Debug.Log("[GameWS] Sent: " + json);
        }

        private async void OnDestroy()
        {
            // 수동 종료 플래그를 세워서 재접속을 막음
            manualClose = true;
            if (websocket != null)
            {
                await websocket.Close();
                websocket = null;
            }
            Instance = null;
        }
    }
}
