using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using MCRGame.UI;
using TMPro;

namespace MCRGame.Net
{
    public class LobbyInfoManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text nicknameText; // 닉네임을 표시할 Text
        [SerializeField] private GameObject CharacterImage;
        [SerializeField] private GameObject ProfileImage;

        private string getUserInfoUrl = CoreServerConfig.GetHttpUrl("/user/me");
        private void Start()
        {
            // 로비씬에 들어오면 유저 정보를 요청
            StartCoroutine(GetUserInfoFromServer());
        }

        private IEnumerator GetUserInfoFromServer()
        {
            // AccessToken이 저장되어 있어야 함
            string token = PlayerDataManager.Instance.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[LobbyInfoManager] 토큰이 없습니다. 로그인이 안 된 상태인가요?");
                yield break;
            }

            using (UnityWebRequest www = UnityWebRequest.Get(getUserInfoUrl))
            {
                // 인증 헤더 추가
                www.SetRequestHeader("Authorization", $"Bearer {token}");
                www.certificateHandler = new BypassCertificateHandler();
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[LobbyInfoManager] 유저 정보 가져오기 성공: " + www.downloadHandler.text);

                    // JSON 파싱
                    UserMeResponse userData = JsonUtility.FromJson<UserMeResponse>(www.downloadHandler.text);

                    // PlayerDataManager에 저장
                    PlayerDataManager.Instance.SetUserData(userData.uid, userData.nickname, userData.email);

                    PlayerDataManager.Instance.SetCharacterData(new List<string> {"default", "c1", "c4", "c5"}, "default");
                    
                    CharacterImage.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_sprite_by_name(PlayerDataManager.Instance.CurrentCharacter);
                    CharacterImage.GetComponent<Image>().color = new Color(255, 255, 255, 255);
                    ProfileImage.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_pfp_by_name(PlayerDataManager.Instance.CurrentCharacter);
                    ProfileImage.GetComponent<Image>().color = new Color(255, 255, 255, 255);
                    // UI에 닉네임 표시
                    nicknameText.text = userData.nickname;
                }
                else
                {
                    Debug.LogError("[LobbyInfoManager] 유저 정보 가져오기 실패: " + www.error);
                }
            }
        }
    }


}