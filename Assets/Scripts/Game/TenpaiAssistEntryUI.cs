using UnityEngine;
using UnityEngine.UI;

namespace MCRGame.Game
{
    [RequireComponent(typeof(LayoutElement))]
    public class TenpaiAssistEntryUI : MonoBehaviour
    {
        [SerializeField] private Image tileImage;
        [SerializeField] private Text tsumoScoreText;
        [SerializeField] private Text discardScoreText;

        // LayoutElement 는 Awake() 에서 확보됩니다.
        private LayoutElement _layoutElement;

        void Awake()
        {
            _layoutElement = GetComponent<LayoutElement>();
            _layoutElement.flexibleWidth = 0;
            _layoutElement.flexibleHeight = 0;

            // 항상 비율 유지
            tileImage.preserveAspect = true;

            // Rich Text 허용
            tsumoScoreText.supportRichText = true;
            discardScoreText.supportRichText = true;

            // 텍스트 기본 정렬
            // tsumoScoreText.alignment   = TextAnchor.LowerCenter;
            // discardScoreText.alignment = TextAnchor.LowerCenter;
        }

        /// <summary>
        /// 일반 엔트리 세팅 (점수 + 부족)
        /// </summary>
        public void Setup(
            Sprite spr,
            int tsumoScore,
            int discardScore,
            bool isLow,
            int scoreLimit
        )
        {
            // --- 이미지 & 색 설정 ---
            tileImage.sprite = spr;
            tileImage.color = isLow ? Color.gray : Color.white;

            // --- 점수 + 부족 텍스트 두 줄 포맷 ---
            int baseSize = tsumoScoreText.fontSize;
            int halfSize = Mathf.Max(1, baseSize / 2);

            string lackT = tsumoScore < scoreLimit
                ? $"<size={halfSize}>부족</size>"
                : "";
            tsumoScoreText.text = $"{tsumoScore}\n{lackT}";
            tsumoScoreText.color = tsumoScore < scoreLimit ? Color.gray : Color.white;

            string lackD = discardScore < scoreLimit
                ? $"<size={halfSize}>부족</size>"
                : "";
            discardScoreText.text = $"{discardScore}\n{lackD}";
            discardScoreText.color = discardScore < scoreLimit ? Color.gray : Color.white;

            // --- 높이 재계산 (이미지 높이 + 텍스트 높이 + 패딩) ---
            LayoutRebuilder.ForceRebuildLayoutImmediate(tsumoScoreText.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(discardScoreText.rectTransform);

            float imageH = ((RectTransform)tileImage.transform).rect.height;
            float textH = Mathf.Max(
                tsumoScoreText.preferredHeight,
                discardScoreText.preferredHeight
            );
            float padding = 4f;
            float totalH = imageH + textH + padding;

            _layoutElement.minHeight = totalH;
            _layoutElement.preferredHeight = totalH;

            // width 는 레이아웃 그룹이 결정하므로 여기서는 만지지 않습니다
        }

        /// <summary>
        /// 더미 entry용: 스프라이트와 "쯔모"/"론" 레이블을 받아 처리
        /// </summary>
        public void SetupDummyEntry(
            Sprite dummySprite,
            string tsumoLabel,
            string ronLabel
        )
        {
            // 1) 타일 이미지는 투명하게
            tileImage.sprite = dummySprite;
            tileImage.color = new Color(1f, 1f, 1f, 0f);

            // 2) '쯔모' 레이블 (굵게, 아래 정렬)
            tsumoScoreText.text = $"<b>{tsumoLabel}</b>\n";
            tsumoScoreText.color = Color.white;

            // 3) '론' 레이블
            discardScoreText.text = $"<b>{ronLabel}</b>\n";
            discardScoreText.color = Color.white;

            // 4) 높이 강제 계산 (이미 투명 이미지라 높이만 확보)
            LayoutRebuilder.ForceRebuildLayoutImmediate(tsumoScoreText.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(discardScoreText.rectTransform);

            float imageH = ((RectTransform)tileImage.transform).rect.height;
            float textH = Mathf.Max(
                tsumoScoreText.preferredHeight,
                discardScoreText.preferredHeight
            );
            float padding = 4f;
            float totalH = imageH + textH + padding;

            _layoutElement.minHeight = totalH;
            _layoutElement.preferredHeight = totalH;
        }
    }
}
