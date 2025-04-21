using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using MCRGame.Net;
using System.Linq;
using Newtonsoft.Json;

namespace MCRGame.Net
{
    public class RoomApiManager : MonoBehaviour
    {
        // CoreServerConfig를 사용하여 기본 방 URL 구성 (예: http://localhost:8000/api/v1/room)
        private string baseRoomUrl = CoreServerConfig.GetHttpUrl("/room");
        public static RoomApiManager Instance { get; private set; }

        private void Awake()
        {
            // 싱글톤 인스턴스 관리
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // URL 초기화
            baseRoomUrl = CoreServerConfig.GetHttpUrl("/room");
        }

        public IEnumerator FetchRoomUsers(
                   string roomNumber,
                   Action<RoomUsersResponse> onSuccess,
                   Action<string> onError
               )
        {
            string url = $"{baseRoomUrl}/{roomNumber}/users";
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
            req.certificateHandler = new BypassCertificateHandler();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // JsonConvert로 바로 파싱
                    var json = req.downloadHandler.text;
                    var data = JsonConvert.DeserializeObject<RoomUsersResponse>(json);
                    onSuccess?.Invoke(data);
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"[RoomApiManager] JSON 파싱 실패: {ex.Message}");
                    onError?.Invoke($"파싱 오류: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"[RoomApiManager] FetchRoomUsers 실패: {req.error}");
                onError?.Invoke(req.error);
            }
        }

        /// <summary>
        /// 서버에서 방 목록을 가져옵니다.
        /// 서버 응답이 JSON 배열 형태라면, JsonUtility의 제한을 위해 래퍼 객체로 감싸서 파싱합니다.
        /// </summary>
        /// <param name="onSuccess">성공 시 AvailableRoomResponseList 반환</param>
        /// <param name="onError">실패 시 오류 메시지 반환</param>
        public IEnumerator FetchRooms(Action<AvailableRoomResponseList> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(baseRoomUrl))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
                request.certificateHandler = new BypassCertificateHandler();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log("[RoomApiManager] Fetched rooms: " + jsonResponse);

                    // 서버 응답이 JSON 배열일 경우, JsonUtility로 파싱하려면 래퍼 객체로 감쌉니다.
                    string wrappedJson = "{\"rooms\":" + jsonResponse + "}";
                    AvailableRoomResponseList roomList = JsonUtility.FromJson<AvailableRoomResponseList>(wrappedJson);

                    onSuccess?.Invoke(roomList);
                }
                else
                {
                    Debug.LogError("[RoomApiManager] FetchRooms failed: " + request.error);
                    onError?.Invoke(request.error);
                }
            }
        }

        public IEnumerator LeaveRoom(string roomId, Action<BaseResponse> onSuccess, Action<string> onError)
        {
            if (!int.TryParse(roomId, out int roomNumber))
            {
                onError?.Invoke("Invalid room number");
                yield break;
            }

            string url = $"{baseRoomUrl}/{roomNumber}/leave";

            // --- WWWForm 을 이용한 POST 호출 ---
            var form = new WWWForm();
            using var request = UnityWebRequest.Post(url, form);
            // 인증 헤더만 따로 설정
            request.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
            request.certificateHandler = new BypassCertificateHandler();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<BaseResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(resp);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }

        /// <summary>
        /// 지정된 방 ID로 방 참가 API를 호출합니다.
        /// roomId는 문자열(내부적으로 int로 변환)이며, 성공 시 RoomResponse 객체를 callback으로 반환합니다.
        /// </summary>
        /// <param name="roomId">방 ID (문자열)</param>
        /// <param name="callback">성공 시 RoomResponse를 반환, 실패하면 null</param>
        public IEnumerator JoinRoom(string roomId, Action<RoomResponse> callback)
        {
            if (!int.TryParse(roomId, out int roomNumber))
            {
                Debug.LogError("[RoomApiManager] Invalid roomId (cannot parse to int).");
                callback?.Invoke(null);
                yield break;
            }

            // 최종 API 엔드포인트: /api/v1/room/{roomNumber}/join
            string joinUrl = $"{baseRoomUrl}/{roomNumber}/join";
            Debug.Log($"[RoomApiManager] Joining room at: {joinUrl}");

            using (UnityWebRequest request = new UnityWebRequest(joinUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(new byte[0]);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
                request.certificateHandler = new BypassCertificateHandler();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[RoomApiManager] Successfully joined room {roomId}. Response: {request.downloadHandler.text}");
                    RoomResponse response = JsonUtility.FromJson<RoomResponse>(request.downloadHandler.text);
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[RoomApiManager] Failed to join room {roomId}. Error: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 방 생성 API를 호출하여 새 방을 생성합니다.
        /// 생성 성공 시, CreateRoomResponse 객체를 반환합니다.
        /// </summary>
        /// <param name="onSuccess">성공 시 CreateRoomResponse 반환</param>
        /// <param name="onError">실패 시 오류 메시지 반환</param>
        public IEnumerator CreateRoom(Action<CreateRoomResponse> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = new UnityWebRequest(baseRoomUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(new byte[0]);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
                request.certificateHandler = new BypassCertificateHandler();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[RoomApiManager] Room created successfully: " + request.downloadHandler.text);
                    CreateRoomResponse response = JsonUtility.FromJson<CreateRoomResponse>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    Debug.LogError("[RoomApiManager] Room creation failed: " + request.error);
                    onError?.Invoke(request.error);
                }
            }
        }
    }
}
