using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using MCRGame.Common;
using MCRGame.Game;

namespace MCRGame.UI
{
    public class TileManager : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        private string tileName;
        private RectTransform imageFieldRect;
        private Image imageComponent;
        private Vector2 originalPos;
        private GameHandManager gameHandManager;

        void Awake()
        {
            gameHandManager = GetComponentInParent<GameHandManager>();

            var imageField = transform.Find("ImageField");
            if (imageField != null)
            {
                imageFieldRect = imageField.GetComponent<RectTransform>();
                imageComponent = imageField.GetComponent<Image>();
                originalPos = imageFieldRect.anchoredPosition;
            }
        }

        public void SetTileName(string newName)
        {
            tileName = newName;
            gameObject.name = newName;
            UpdateSprite();
        }

        // 더 이상 Update() 에서 Raycast 하지 않습니다.

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!gameHandManager.CanHover) return;

            // 1) 비주얼 이펙트
            float moveUp = imageFieldRect.rect.height / 3f;
            imageFieldRect.anchoredPosition = originalPos + new Vector2(0, moveUp);

            GameManager.Instance.NowHoverTile = null;
            GameManager.Instance.NowHoverSource = null;
            // 2) hover 상태 등록
            if (GameTileExtensions.TryParseCustom(tileName, out var tile))
            {
                GameManager.Instance.NowHoverTile = tile;
                GameManager.Instance.NowHoverSource = this;

                gameHandManager.DiscardManager.HighlightTiles(tile);
                foreach (var cbField in GameManager.Instance.CallBlockFields)
                    cbField.HighlightBlocks(tile);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!gameHandManager.CanHover) return;

            // 1) 비주얼 복원
            imageFieldRect.anchoredPosition = originalPos;

            // 2) 자신이 마지막 등록된 hover source 였을 때만 해제
            if (GameManager.Instance.NowHoverSource == this)
            {
                GameManager.Instance.NowHoverTile = null;
                GameManager.Instance.NowHoverSource = null;
            }

            gameHandManager.DiscardManager.ClearHighlights();
            foreach (var cbField in GameManager.Instance.CallBlockFields)
                cbField.ClearHighlights();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!GameManager.Instance.CanClick) return;
            GameManager.Instance.CanClick = false;
            gameHandManager.RequestDiscard(this);
        }

        private void UpdateSprite()
        {
            if (string.IsNullOrEmpty(tileName) || imageComponent == null) return;
            var spr = Tile2DManager.Instance.get_sprite_by_name(tileName);
            if (spr != null)
            {
                imageComponent.sprite = spr;
                imageComponent.color = Color.white;
            }
        }

        public void UpdateTransparent()
        {
            if (imageComponent != null)
                imageComponent.color = new Color(1f, 1f, 1f, 0f);
        }

        public void ResetPosition()
        {
            if (imageFieldRect != null)
                imageFieldRect.anchoredPosition = originalPos;
        }

        void OnDestroy()
        {
            if (GameManager.Instance.NowHoverSource == this)
            {
                GameManager.Instance.NowHoverTile = null;
                GameManager.Instance.NowHoverSource = null;

                gameHandManager.DiscardManager.ClearHighlights();
                foreach (var cbField in GameManager.Instance.CallBlockFields)
                    cbField.ClearHighlights();
            }
        }
    }
}
