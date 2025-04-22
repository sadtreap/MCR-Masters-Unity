using System;
using System.Linq;
using System.Collections; // 코루틴 사용을 위한 네임스페이스
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace MCRGame.Net
{
    public class RoomManager : MonoBehaviour
    {
        public Button actionButton;
        public Button backButton;
        public Image[] readyIndicators;
        public Text roomTitleText;
        public Text roomNumberText;
        public RoomWS roomWS;
        public Text[] playerNicknameTexts;
        public Image[] playerCharacterImages;
        public Sprite defaultCharacterSprite;

        private bool isHost = false;
        private bool isReady = false;
        private bool wsConnected = false;
        private bool clickedReadyBeforeWS = false;
        private float _lastUIUpdateTime = -Mathf.Infinity;

        void Start()
        {
            // 방 제목/번호 표시
            if (RoomDataManager.Instance != null)
            {
                roomTitleText.text = RoomDataManager.Instance.RoomTitle;
                roomNumberText.text = RoomDataManager.Instance.RoomId;
            }

            // 최초 사용자 목록 불러오기
            StartCoroutine(
                RoomApiManager.Instance.FetchRoomUsers(
                    RoomDataManager.Instance.RoomId,
                    resp =>
                    {
                        foreach (var u in resp.users)
                            RoomDataManager.Instance.AddOrUpdateUser(u);
                        RoomDataManager.Instance.OnHostChanged(resp.host_uid);
                        UpdateAllPlayerStatus();
                        UpdatePlayerUI();
                    },
                    err => Debug.LogError("FetchRoomUsers failed: " + err)
                )
            );

            backButton.onClick.AddListener(OnBackButton);

            // 내 호스트 여부 판단 및 초기 Players 세팅
            if (PlayerDataManager.Instance != null && RoomDataManager.Instance != null)
            {
                isHost = PlayerDataManager.Instance.Uid == RoomDataManager.Instance.HostUser.uid;
                var players = RoomDataManager.Instance.Players;

                if (isHost)
                {
                    RoomDataManager.Instance.mySlotIndex = RoomDataManager.Instance.HostSlotIndex;
                    players[RoomDataManager.Instance.mySlotIndex] = new RoomUserData
                    {
                        uid = PlayerDataManager.Instance.Uid,
                        nickname = PlayerDataManager.Instance.Nickname,
                        isReady = true,
                        slot_index = RoomDataManager.Instance.mySlotIndex
                    };
                    isReady = true;
                }
                else
                {
                    int mySlot = RoomDataManager.Instance.mySlotIndex;
                    if (mySlot >= 0 && mySlot < players.Length)
                    {
                        players[mySlot] = new RoomUserData
                        {
                            uid = PlayerDataManager.Instance.Uid,
                            nickname = PlayerDataManager.Instance.Nickname,
                            isReady = false,
                            slot_index = mySlot
                        };
                    }
                }
            }
            else
            {
                Debug.LogError("PlayerDataManager 또는 RoomDataManager 인스턴스가 없습니다.");
            }

            // Action 버튼 초기 설정
            if (actionButton != null)
            {
                var txt = actionButton.GetComponentInChildren<Text>();
                if (isHost)
                {
                    txt.text = "Start";
                    actionButton.interactable = false;
                    isReady = true;
                }
                else
                {
                    txt.text = "Ready";
                    actionButton.interactable = true;
                }
                actionButton.onClick.AddListener(OnActionButtonClicked);
            }

            UpdatePlayerUI();

            if (roomWS != null)
                roomWS.OnWebSocketConnected += OnWebSocketConnectedHandler;
        }

        private void OnDestroy()
        {
            if (roomWS != null)
                roomWS.OnWebSocketConnected -= OnWebSocketConnectedHandler;
        }

        #region Host / Leave

        public void OnHostChanged(string newHostUid)
        {
            var rdm = RoomDataManager.Instance;
            for (int i = 0; i < rdm.Players.Length; i++)
            {
                if (rdm.Players[i]?.uid == newHostUid)
                {
                    rdm.HostUser = rdm.Players[i];
                    rdm.HostSlotIndex = i;
                    break;
                }
            }

            isHost = PlayerDataManager.Instance.Uid == newHostUid;
            var btnText = actionButton.GetComponentInChildren<Text>();
            if (isHost)
            {
                isReady = true;
                btnText.text = "Start";
                actionButton.interactable = rdm.Players.All(p => p.isReady);
            }
            else
            {
                btnText.text = "Ready";
                actionButton.interactable = true;
            }
        }

        private void OnBackButton()
        {
            StartCoroutine(
                RoomApiManager.Instance.LeaveRoom(
                    RoomDataManager.Instance.RoomId,
                    onSuccess: resp =>
                    {
                        Debug.Log("[RoomManager] 방 나가기 성공: " + resp.message);
                        SceneManager.LoadScene("RoomListScene");
                    },
                    onError: err =>
                    {
                        Debug.LogError("[RoomManager] 방 나가기 실패: " + err);
                        SceneManager.LoadScene("RoomListScene");
                    }
                )
            );
        }

        #endregion

        #region WebSocket

        private void OnWebSocketConnectedHandler()
        {
            wsConnected = true;
            Debug.Log("[RoomManager] WebSocket 연결 완료");
            if (isHost || (!isHost && clickedReadyBeforeWS && roomWS != null))
            {
                roomWS.SendReadyStatus(isReady);
                Debug.Log("[RoomManager] 연동 후 Ready 상태 전송: " + isReady);
            }
        }

        #endregion

        #region Action Button

        private void OnActionButtonClicked()
        {
            if (isHost)
            {
                OnHostStartGame();
            }
            else
            {
                // 게스트 Ready 토글
                isReady = !isReady;
                int slot = RoomDataManager.Instance.mySlotIndex;
                RoomDataManager.Instance.Players[slot].isReady = isReady;

                // 로컬 UI 즉시 갱신
                RefreshPlayerUI_Internal();

                actionButton.GetComponentInChildren<Text>().text = isReady ? "Ready ✔" : "Ready";

                if (wsConnected && roomWS != null)
                    roomWS.SendReadyStatus(isReady);
                else
                    clickedReadyBeforeWS = true;
            }
        }

        public void OnHostStartGame()
        {
            if (roomWS != null)
                roomWS.SendReadyStatus(isReady);

            int readyCount = RoomDataManager.Instance.Players.Count(p => p.isReady);
            Debug.Log($"[RoomManager] Host Start 클릭: {readyCount}/4 준비됨");

            if (readyCount == RoomDataManager.Instance.Players.Length)
                StartCoroutine(CallStartGameApi());
            else
                Debug.LogWarning("아직 모든 플레이어가 준비되지 않았습니다.");
        }

        private IEnumerator CallStartGameApi()
        {
            string url = CoreServerConfig.GetHttpUrl($"/room/{RoomDataManager.Instance.RoomId}/game-start");
            using var req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Array.Empty<byte>()),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
            req.certificateHandler = new BypassCertificateHandler();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log("Start game API 성공: " + req.downloadHandler.text);
            else
                Debug.LogError("Start game API 실패: " + req.error);
        }

        #endregion

        #region UI 업데이트

        public void UpdateAllPlayerStatus()
        {
            var players = RoomDataManager.Instance.Players;
            if (players == null) return;
            foreach (var p in players.Where(p => p != null))
                UpdatePlayerReadyState(p.uid, p.isReady);
        }

        public void UpdatePlayerReadyState(string uid, bool readyStatus)
        {
            var rdm = RoomDataManager.Instance;
            var players = rdm.Players;

            // 1) 호스트면 HostSlotIndex 처리
            if (rdm.HostUser != null && uid == rdm.HostUser.uid)
            {
                players[rdm.HostSlotIndex].isReady = readyStatus;
                Debug.Log($"[RoomManager] Host Ready → {readyStatus}");
            }
            else
            {
                // 2) 이미 있는 슬롯 찾기
                int guestSlot = Array.FindIndex(players, p => p != null && p.uid == uid);

                // 3) 없으면 빈 슬롯(host 슬롯 제외)에 신규 생성
                if (guestSlot == -1)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (i == rdm.HostSlotIndex) continue;
                        if (players[i] == null)
                        {
                            players[i] = new RoomUserData
                            {
                                uid = uid,
                                nickname = "Unknown",
                                isReady = readyStatus,
                                slot_index = i
                            };
                            guestSlot = i;
                            break;
                        }
                    }
                }

                // 4) 최종 ready 상태 적용
                if (guestSlot != -1)
                {
                    players[guestSlot].isReady = readyStatus;
                    Debug.Log($"[RoomManager] Guest({uid}) Ready → {readyStatus} (slot {guestSlot})");
                }
            }

            // 5) UI 갱신
            RefreshPlayerUI_Internal();

            // 6) Host라면 Start 버튼 활성화 체크
            if (isHost)
            {

                bool allReady = RoomDataManager.Instance.Players.All(p => p != null && p.isReady);
                actionButton.interactable = allReady;
            }
        }

        public void UpdatePlayerUI()
        {
            // 1초 이내 중복 호출 방지
            if (Time.time - _lastUIUpdateTime < 1f) return;
            _lastUIUpdateTime = Time.time;

            StartCoroutine(FetchAndRefreshPlayerUI());
            RoomDataManager.Instance.PrintPlayers();
        }

        private IEnumerator FetchAndRefreshPlayerUI()
        {
            string roomId = RoomDataManager.Instance.RoomId;
            Debug.Log($"[RoomManager] FetchAndRefreshPlayerUI 시작. RoomId={roomId}");
            bool done = false;
            float startTime = Time.time;

            yield return RoomApiManager.Instance.FetchRoomUsers(
                roomId,
                resp =>
                {
                    if (RoomDataManager.Instance.HostUser.uid != resp.host_uid)
                        OnHostChanged(resp.host_uid);
                    Debug.Log($"[RoomManager] FetchRoomUsers 성공: users.Count={resp.users.Length}");
                    foreach (var u in resp.users)
                    {
                        RoomDataManager.Instance.AddOrUpdateUser(u);
                        Debug.Log($"[RoomManager] AddOrUpdateUser 호출: uid={u.uid}, nickname={u.nickname}, isReady={u.isReady}, slot_index={u.slot_index}");
                    }
                    UpdateAllPlayerStatus();
                    Debug.Log("[RoomManager] UpdateAllPlayerStatus 호출 완료");
                    done = true;
                },
                err =>
                {
                    Debug.LogError($"[RoomManager] FetchRoomUsers 실패: {err}");
                    done = true;
                }
            );

            while (!done)
            {
                Debug.Log("[RoomManager] FetchAndRefreshPlayerUI 대기 중...");
                yield return null;
            }

            float elapsed = Time.time - startTime;
            Debug.Log($"[RoomManager] FetchAndRefreshPlayerUI 완료 (소요 {elapsed:F2}s). 이제 UI 갱신합니다.");
            RefreshPlayerUI_Internal();
            Debug.Log("[RoomManager] RefreshPlayerUI_Internal 호출 완료");
        }


        private void ClearAllPlayerUI()
        {
            foreach (var t in playerNicknameTexts) { t.text = ""; t.gameObject.SetActive(false); }
            foreach (var img in playerCharacterImages) { img.sprite = null; img.gameObject.SetActive(false); }
            foreach (var ind in readyIndicators) { ind.gameObject.SetActive(false); }
        }

        private void RefreshPlayerUI_Internal()
        {
            ClearAllPlayerUI();
            var players = RoomDataManager.Instance.Players;
            for (int i = 0; i < players.Length; i++)
            {
                var u = players[i];
                if (u == null) continue;

                playerNicknameTexts[i].text = u.nickname;
                playerNicknameTexts[i].gameObject.SetActive(true);

                playerCharacterImages[i].sprite = defaultCharacterSprite;
                playerCharacterImages[i].gameObject.SetActive(true);

                readyIndicators[i].gameObject.SetActive(true);
                readyIndicators[i].color = u.isReady ? Color.green : Color.red;
            }

            if (isHost)
            {
                bool allReady = RoomDataManager.Instance.Players.All(p => p != null && p.isReady);
                actionButton.interactable = allReady;
            }
        }

        #endregion
    }
}
