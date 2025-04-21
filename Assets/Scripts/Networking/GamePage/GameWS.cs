using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using MCRGame.UI;
using MCRGame.Game;

namespace MCRGame.Net
{
    public class GameWS : MonoBehaviour
    {
        public static GameWS Instance { get; private set; }
        private WebSocket websocket;

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
            Connect();
        }

        private async void Connect()
        {
            // 1) URL, 토큰 준비
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

            // 2) WebGL 한계로 쿼리스트링으로 전달
            string url = $"{baseUrl}?user_id={Uri.EscapeDataString(uid)}" +
                         $"&nickname={Uri.EscapeDataString(nick)}";
                        // + $"&authorization={Uri.EscapeDataString(token)}";

            // 3) NativeWebSocket 사용
            websocket = new WebSocket(url);

            websocket.OnOpen += () =>
            {
                Debug.Log("[GameWS] WebSocket connected!");
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError("[GameWS] WebSocket Error: " + e);
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log($"[GameWS] WebSocket Closed: {e}");
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

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        }

        /// <summary>
        /// 서버가 기대하는 { event, data } 구조로 전송합니다.
        /// </summary>
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
            if (websocket != null)
            {
                await websocket.Close();
                websocket = null;
            }
            Instance = null;
        }
    }
}
