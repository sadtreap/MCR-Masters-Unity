using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MCRGame.Net;
using System.Collections;
using UnityEngine.Networking;

namespace MCRGame.UI
{
    public class CharacterChange : MonoBehaviour
    {
        public GameObject CharacterPanelPrefab;
        public GameObject CharacterSelectButtonPrefab;

        [SerializeField] private GameObject CharacterImage;
        [SerializeField] private GameObject ProfileImage;

        public void RevealCharacterPanel(bool closePanel = true)
        {
            GameObject oldPanel = GameObject.Find("ListOfCharacters(Clone)");
            if (oldPanel != null)
            {
                Destroy(oldPanel);
                oldPanel = null;
                if(closePanel) return;
            }
            GameObject CharacterPanel = Instantiate(CharacterPanelPrefab, transform);
            List<CharacterResponse> ownedCharacters = PlayerDataManager.Instance.OwnedCharacters;
            foreach (var character in ownedCharacters)
            {
                string characterCode = character.code;
                GameObject b = Instantiate(CharacterSelectButtonPrefab, CharacterPanel.transform);
                b.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_pfp_by_code(characterCode);
                b.GetComponent<Button>().onClick.AddListener(() => ChangeCharacter(characterCode));
                if (characterCode == PlayerDataManager.Instance.CurrentCharacter) b.GetComponent<Button>().interactable = false;
            }
        }

        private void ChangeCharacter(string characterCode)
        {
            StartCoroutine(ChangeServerCharacter(characterCode));
            Debug.Log($"[CharacterChange] 캐릭터를 {characterCode}로 변경했습니다.");
            PlayerDataManager.Instance.SetCurrentCharacter(characterCode);
            CharacterImage.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_sprite_by_code(characterCode);
            ProfileImage.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_pfp_by_code(characterCode);
            RevealCharacterPanel(false);
        }

        private IEnumerator ChangeServerCharacter(string code)
        {
            var req = new UnityWebRequest(CoreServerConfig.GetHttpUrl("/user/me/character/" + code), "PUT");
            string token = PlayerDataManager.Instance.AccessToken;
            req.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return req.SendWebRequest();
        }
    }
}