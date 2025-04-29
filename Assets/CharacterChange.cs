using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MCRGame.Net;

namespace MCRGame.UI
{
    public class CharacterChange : MonoBehaviour
    {
        public GameObject CharacterPanelPrefab;
        public GameObject CharacterSelectButtonPrefab;

        [SerializeField] private GameObject CharacterImage;
        [SerializeField] private GameObject ProfileImage;

        private bool isActive = false;

        /// <summary>
        /// 버튼이 클릭되었을 때, 해당 씬으로 이동
        /// </summary>
        public void RevealCharacterPanel()
        {
            GameObject oldPanel = GameObject.Find("ListOfCharacters(Clone)");
            if (oldPanel != null)
            {
                Destroy(oldPanel);
                oldPanel = null;
            }
            GameObject CharacterPanel = Instantiate(CharacterPanelPrefab, transform);
            List<string> ownedCharacters = PlayerDataManager.Instance.OwnedCharacters;
            foreach (string characterName in ownedCharacters)
            {
                
                GameObject b = Instantiate(CharacterSelectButtonPrefab, CharacterPanel.transform);
                b.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_pfp_by_name(characterName);
                b.GetComponent<Button>().onClick.AddListener(() => ChangeCharacter(characterName));
                if (characterName == PlayerDataManager.Instance.CurrentCharacter) b.GetComponent<Button>().interactable = false;
            }
        }

        private void ChangeCharacter(string characterName)
        {
            PlayerDataManager.Instance.SetCurrentCharacter(characterName);
            CharacterImage.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_sprite_by_name(characterName);
            ProfileImage.GetComponent<Image>().sprite = CharacterImageManager.Instance.get_character_pfp_by_name(characterName);
            RevealCharacterPanel();
        }
    }
}