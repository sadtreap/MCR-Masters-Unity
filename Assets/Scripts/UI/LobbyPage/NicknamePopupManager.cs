using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using MCRGame.Net;

namespace MCRGame.UI
{
    public class NicknamePopupManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;          // 팝업 전체 Panel
        [SerializeField] private InputField nicknameInput;       // 새 닉네임 입력창
        [SerializeField] private Text currentNicknameText;       // 로비에 표시중인 닉네임 텍스트
        [SerializeField] private Button confirmButton;           // 변경 확인 버튼
        [SerializeField] private Text errorText;                 // 에러 메시지 표시용(Text)

        [Header("Dependencies")]
        [SerializeField] private PutNicknameApi putNicknameApi;  // 이전에 구현한 PUT API 컴포넌트

        private void Awake()
        {
            // 버튼에 리스너 연결
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void Start()
        {
            // new user인 경우에만 팝업을 자동으로 띄웁니다.
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsNewUser)
            {
                popupPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void ClosePopup()
        {
            popupPanel.SetActive(false);
        }

        /// <summary>
        /// 확인 버튼 클릭 시 호출
        /// </summary>
        private void OnConfirmClicked()
        {
            string newNick = nicknameInput.text.Trim();
            if (string.IsNullOrEmpty(newNick))
            {
                errorText.text = "닉네임을 입력해주세요.";
                return;
            }
            // PUT /user/me/nickname
            StartCoroutine(putNicknameApi.UpdateNickname(
                newNick,
                PlayerDataManager.Instance.AccessToken,
                onSuccess: _ => StartCoroutine(FetchUserMe()), 
                onError: err => errorText.text = $"업데이트 실패: {err}"
            ));
            ClosePopup();
        }

        /// <summary>
        /// PUT 성공 후 GET /user/me 해서 PlayerDataManager, UI 갱신
        /// </summary>
        private IEnumerator FetchUserMe()
        {
            string url = CoreServerConfig.GetHttpUrl("/user/me");
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", $"Bearer {PlayerDataManager.Instance.AccessToken}");
                www.certificateHandler = new BypassCertificateHandler();
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // JSON 파싱
                    UserMeResponse userData = JsonUtility.FromJson<UserMeResponse>(www.downloadHandler.text);

                    // PlayerDataManager에 저장
                    PlayerDataManager.Instance.SetUserData(userData.uid, userData.nickname, userData.email);

                    // 로비 UI에 반영
                    currentNicknameText.text = userData.nickname;

                    // 팝업 닫기
                    ClosePopup();
                }
                else
                {
                    errorText.text = $"정보 조회 실패: {www.error}";
                }
            }
        }
    }
}
