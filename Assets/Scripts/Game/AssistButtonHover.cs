using UnityEngine;
using UnityEngine.EventSystems;

namespace MCRGame.Game
{
    /// <summary>
    /// Assist 버튼 위로 마우스가 올라가거나 벗어났을 때
    /// TenpaiAssistDisplay 쪽에 알려 줍니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AssistButtonHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private TenpaiAssistDisplay display;

        public void OnPointerEnter(PointerEventData eventData)
        {
            display.OnAssistButtonEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            display.OnAssistButtonExit();
        }
    }
}
