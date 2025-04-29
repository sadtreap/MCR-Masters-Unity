using System.Collections.Generic;
using UnityEngine;

namespace MCRGame.UI
{
    public class CharacterImageManager : MonoBehaviour
    {
        public static CharacterImageManager Instance { get; private set; }

        private string character_images_path = "Images/CharacterAssets";
        private string character_pfp_path = "Images/CharacterProfile";

        public Dictionary<string, Sprite> character_name_to_sprite;
        public Dictionary<string, Sprite> character_name_to_pfp_sprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Sprite[] characterSprites = Resources.LoadAll<Sprite>(character_images_path);
            Sprite[] pfpSprites = Resources.LoadAll<Sprite>(character_pfp_path);

            character_name_to_sprite = new Dictionary<string, Sprite>();
            character_name_to_pfp_sprite = new Dictionary<string, Sprite>();

            foreach (Sprite sprite in characterSprites)
            {
                if (!character_name_to_sprite.ContainsKey(sprite.name))
                {
                    character_name_to_sprite.Add(sprite.name, sprite);
                }
            }

            foreach (Sprite sprite in pfpSprites)
            {
                if (!character_name_to_pfp_sprite.ContainsKey(sprite.name))
                {
                    character_name_to_pfp_sprite.Add(sprite.name, sprite);
                }
            }

            Debug.Log($"[TileImageManager] {character_name_to_sprite.Count}개의 캐릭터 스프라이트를 로드했습니다.");
            Debug.Log($"[TileImageManager] {character_name_to_pfp_sprite.Count}개의 캐릭터 프로필 스프라이트를 로드했습니다.");
        }

        public Sprite get_character_sprite_by_name(string name)
        {
            if (character_name_to_sprite.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }
            Debug.LogWarning($"[TileImageManager] '{name}' 캐릭터 스프라이트를 찾을 수 없습니다.");
            return null;
        }

        public Sprite get_character_pfp_by_name(string name)
        {
            if (character_name_to_pfp_sprite.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }
            Debug.LogWarning($"[TileImageManager] '{name}' 프로필을 찾을 수 없습니다.");
            return null;
        }
    }
}
