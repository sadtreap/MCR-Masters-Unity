using System;
using System.Text;
using UnityEngine;
using UnityWebSocket;    // psygames/UnityWebSocket
using Newtonsoft.Json;
using MCRGame.UI;
using MCRGame.Game;

namespace MCRGame.Net
{
    public class GameWS : MonoBehaviour
    {
        public static GameWS Instance { get; private set; }
        private WebSocket ws;

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

        private void Connect()
        {
            // 1) URL, 토큰 준비
            string baseUrl = GameServerConfig.GetWebSocketUrl();
            var pdm = PlayerDataManager.Instance;
            string uid   = pdm?.Uid         ?? "";
            string nick  = pdm?.Nickname    ?? "";
            string token = pdm?.AccessToken ?? "";

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[GameWS] AccessToken이 없습니다.");
                return;
            }

            // 2) WebGL 한계로 쿼리스트링으로 전달
            string url = $"{baseUrl}?user_id={Uri.EscapeDataString(uid)}" +
                         $"&nickname={Uri.EscapeDataString(nick)}" +
                         $"&authorization={Uri.EscapeDataString(token)}";

            ws = new WebSocket(url);

            // 3) 이벤트 핸들러 등록 (EventHandler<T> signature)
            ws.OnOpen += OnOpenHandler;
            ws.OnMessage += OnMessageHandler;
            ws.OnError += OnErrorHandler;
            ws.OnClose += OnCloseHandler;

            Debug.Log("[GameWS] Connecting to: " + url);
            ws.ConnectAsync();
        }

        private void OnOpenHandler(object sender, OpenEventArgs args)
        {
            Debug.Log("[GameWS] WebSocket connected!");
        }

        private void OnMessageHandler(object sender, MessageEventArgs args)
        {
            string msg = Encoding.UTF8.GetString(args.RawData);
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
        }

        private void OnErrorHandler(object sender, ErrorEventArgs args)
        {
            Debug.LogError("[GameWS] WebSocket Error: " + args.Message);
        }

        private void OnCloseHandler(object sender, CloseEventArgs args)
        {
            Debug.Log($"[GameWS] WebSocket Closed: {args.Code} / {args.Reason}");
        }

        /// <summary>
        /// 서버가 기대하는 최상위 { event, data } 구조로 전송합니다.
        /// </summary>
        public void SendGameEvent(GameWSActionType action, object payload)
        {
            if (ws == null || ws.ReadyState != WebSocketState.Open)
            {
                Debug.LogWarning("[GameWS] WebSocket is not open; cannot send.");
                return;
            }

            var messageObj = new
            {
                @event = action,
                data   = payload
            };

            string json = JsonConvert.SerializeObject(messageObj);
            ws.SendAsync(json);
            Debug.Log("[GameWS] Sent: " + json);
        }

        private void OnDestroy()
        {
            if (ws != null)
            {
                ws.CloseAsync();
                ws = null;
            }
            Instance = null;
        }
    }
}
