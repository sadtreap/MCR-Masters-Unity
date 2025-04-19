using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MCRGame.Net;
using System.Collections;

namespace MCRGame.UI
{
    public class RoomListManager : MonoBehaviour
    {
        public static RoomListManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject scrollViewPrefab;
        [SerializeField] private GameObject roomItemPrefab;

        [Header("UI References")]
        [SerializeField] private Button refreshButton;      // inspector에서 드래그할당

        private GameObject scrollViewInst;
        private Transform contentParent;

        [Header("Settings")]
        [SerializeField] private float refreshInterval = 5f;

        private Coroutine autoRefreshCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("[RoomListManager] Singleton Awake and listening for sceneLoaded.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[RoomListManager] Scene loaded: {scene.name}");

            if (scene.name == "RoomListScene")
            {
                InitScrollView();
                RefreshRoomList();
                StartAutoRefresh();
            }
            else
            {
                // RoomListScene이 아니면 자동 갱신 중지 및 UI 제거
                if (autoRefreshCoroutine != null)
                {
                    Debug.Log("[RoomListManager] Exiting RoomListScene → Stop AutoRefresh");
                    StopCoroutine(autoRefreshCoroutine);
                    autoRefreshCoroutine = null;
                }
                if (scrollViewInst != null)
                {
                    Debug.Log("[RoomListManager] Exiting RoomListScene → Destroy ScrollView");
                    Destroy(scrollViewInst);
                    scrollViewInst = null;
                    contentParent = null;
                }
            }
        }

        private void InitScrollView()
        {
            Debug.Log("[RoomListManager] InitScrollView() 시작");

            if (scrollViewInst != null)
            {
                Debug.Log("[RoomListManager] Destroy existing scrollViewInst");
                Destroy(scrollViewInst);
                scrollViewInst = null;
                contentParent = null;
            }

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[RoomListManager] Canvas를 찾을 수 없습니다!");
                return;
            }
            Debug.Log($"[RoomListManager] Canvas found: {canvas.gameObject.name}");

            if (scrollViewPrefab == null)
            {
                Debug.LogError("[RoomListManager] scrollViewPrefab이 할당되지 않았습니다!");
                return;
            }
            scrollViewInst = Instantiate(scrollViewPrefab, canvas.transform, false);
            scrollViewInst.name = "RoomListScrollView";
            Debug.Log($"[RoomListManager] scrollViewInst created: {scrollViewInst.name}");

            var viewport = scrollViewInst.transform.Find("Viewport");
            if (viewport == null)
            {
                Debug.LogError("[RoomListManager] scrollViewInst에 'Viewport' 자식이 없습니다!");
                return;
            }
            Debug.Log("[RoomListManager] Viewport found");

            var content = viewport.Find("Content");
            if (content == null)
            {
                Debug.LogError("[RoomListManager] Viewport에 'Content' 자식이 없습니다!");
                return;
            }
            Debug.Log("[RoomListManager] Content found");
            contentParent = content;

            if (refreshButton == null)
            {
                Debug.LogWarning("[RoomListManager] refreshButton이 inspector에 할당되지 않았습니다!");
            }
            else
            {
                Debug.Log($"[RoomListManager] refreshButton found: {refreshButton.gameObject.name}");
                refreshButton.onClick.RemoveAllListeners();
                refreshButton.onClick.AddListener(RefreshRoomList);
            }

            Debug.Log("[RoomListManager] InitScrollView() completed");
        }

        private void StartAutoRefresh()
        {
            if (autoRefreshCoroutine != null)
                StopCoroutine(autoRefreshCoroutine);
            Debug.Log("[RoomListManager] StartAutoRefresh()");
            autoRefreshCoroutine = StartCoroutine(AutoRefreshRoomList());
        }

        private IEnumerator AutoRefreshRoomList()
        {
            while (true)
            {
                yield return new WaitForSeconds(refreshInterval);
                RefreshRoomList();
            }
        }

        public void RefreshRoomList()
        {
            Debug.Log("[RoomListManager] RefreshRoomList() 호출");
            StartCoroutine(RoomApiManager.Instance.FetchRooms(OnRoomsFetched, OnFetchError));
        }

        private void OnRoomsFetched(AvailableRoomResponseList roomListResponse)
        {
            Debug.Log($"[RoomListManager] OnRoomsFetched: rooms count = {roomListResponse?.rooms?.Length}");
            if (contentParent == null)
            {
                Debug.LogError("[RoomListManager] contentParent is null!");
                return;
            }

            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            if (roomListResponse?.rooms == null || roomListResponse.rooms.Length == 0)
            {
                Debug.Log("[RoomListManager] No rooms returned from API.");
                return;
            }

            foreach (var roomInfo in roomListResponse.rooms)
            {
                Debug.Log($"[RoomListManager] Creating RoomItem for room {roomInfo.room_number}");
                CreateRoomItem(roomInfo);
            }
        }

        private void OnFetchError(string error)
        {
            Debug.LogError("[RoomListManager] FetchRooms error: " + error);
        }

        private void CreateRoomItem(AvailableRoomResponse roomInfo)
        {
            if (roomItemPrefab == null)
            {
                Debug.LogError("[RoomListManager] roomItemPrefab이 할당되지 않았습니다!");
                return;
            }
            Debug.Log($"[RoomListManager] Instantiating roomItemPrefab: {roomItemPrefab.name}");
            var item = Instantiate(roomItemPrefab, contentParent);
            Debug.Log($"[RoomListManager] roomItemInst created: {item.name}");

            var ri = item.GetComponent<RoomItem>();
            if (ri != null)
            {
                ri.Setup(roomResponse: roomInfo);
                Debug.Log($"[RoomListManager] RoomItem.Setup completed for room {roomInfo.room_number}");
            }
            else
            {
                Debug.LogError("[RoomListManager] RoomItem component missing on instantiated object!");
            }
        }
    }
}
