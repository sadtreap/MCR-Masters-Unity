using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MCRGame.Net; // RoomDataManager, PlayerDataManager, RoomUserData 등 포함

namespace MCRGame.UI
{
    public class RoomSceneTransition : MonoBehaviour
    {
        public static RoomSceneTransition Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// 방 참가가 완료된 후, RoomScene으로 전환합니다.
        /// 방 정보를 전역 매니저에 저장하고, 이후 RoomScene에서 활용할 수 있습니다.
        /// </summary>
        /// <param name="roomId">방 번호</param>
        /// <param name="roomTitle">방 제목</param>
        /// <param name="hostNickname">호스트의 닉네임</param>
        public void TransitionToRoomScene(string roomId, string roomTitle, string hostNickname)
        {
            Debug.Log($"[RoomSceneTransition] Transitioning to RoomScene. Room ID: {roomId}, Title: {roomTitle}, HostNickname: {hostNickname}");

            // 플레이어 데이터 매니저가 있다면 host uid를 가져옵니다.
            string hostUid = PlayerDataManager.Instance != null ? PlayerDataManager.Instance.Uid : string.Empty;
            if (string.IsNullOrEmpty(hostUid))
                Debug.LogWarning("[RoomSceneTransition] PlayerDataManager 인스턴스가 없거나 Uid가 비어 있습니다.");

            // RoomUserData 객체 생성 (호스트) - host의 슬롯 인덱스는 0으로 설정
            RoomUserData hostUser = new RoomUserData
            {
                uid = hostUid,
                nickname = hostNickname,
                isReady = true, // 호스트는 기본적으로 준비 상태라고 가정
                slot_index = 0
            };

            // 전역 RoomDataManager에 방 정보를 저장합니다.
            if (RoomDataManager.Instance != null)
            {
                RoomDataManager.Instance.SetRoomInfo(roomId, roomTitle, hostUser, 0);
            }
            else
            {
                Debug.LogWarning("[RoomSceneTransition] RoomDataManager 인스턴스가 없습니다.");
            }

            // 단일 모드로 새로운 씬 로드 (기존 씬은 자동 언로드)
            SceneManager.LoadScene("RoomScene", LoadSceneMode.Single);
        }

        /// <summary>
        /// RoomListScene으로 돌아갑니다.
        /// </summary>
        public void ReturnToRoomListScene()
        {
            Debug.Log("[RoomSceneTransition] Returning to RoomListScene");
            SceneManager.LoadScene("RoomListScene", LoadSceneMode.Single);
        }

        /// <summary>
        /// 방 생성 UI를 표시합니다.
        /// </summary>
        /// <param name="roomCreationPanel">활성화할 UI 패널</param>
        public void ShowRoomCreationUI(GameObject roomCreationPanel)
        {
            if (roomCreationPanel != null)
                roomCreationPanel.SetActive(true);
        }
    }
}
