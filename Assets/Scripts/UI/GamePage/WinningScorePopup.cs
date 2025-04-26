using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MCRGame.Common;
using MCRGame.Game;
using MCRGame.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using Unity.Mathematics;
using DG.Tweening;

namespace MCRGame.UI
{
    public class WinningScorePopup : MonoBehaviour
    {
        // 참조할 UI 요소들 (인스펙터에서 할당)
        [Header("UI References")]
        [SerializeField] private GameObject TilePanel;
        [SerializeField] private TextMeshProUGUI singleScoreText;
        [SerializeField] private TextMeshProUGUI totalScoreText;
        [SerializeField] private TextMeshProUGUI winnerNicknameText;
        [SerializeField] private TextMeshProUGUI flowerCountText;
        [SerializeField] private Image characterImage;
        [SerializeField] private Image flowerImage;
        [SerializeField] private Button okButton;
        [SerializeField] private WinningHandDisplay winningHandDisplay;
        [SerializeField] private GameObject winningHandOrigin;
        [SerializeField] private GameObject yakuOrigin;
        [SerializeField] private TextMeshProUGUI scoreTextPrefab;
        [SerializeField] private GameObject yakuObjectPrefab;
        [SerializeField] private GameObject yakuPanel;

        // 팝업 초기화 메서드
        public void Initialize(WinningScoreData scoreData)
        {
            // 점수 표시
            singleScoreText.text = $"{scoreData.singleScore:N0}";
            totalScoreText.text = $"{scoreData.totalScore:N0}";

            winningHandDisplay.ShowWinningHand(scoreData);
            // 승자 정보
            winnerNicknameText.text = GameManager.Instance.Players[GameManager.Instance.seatToPlayerIndex[scoreData.winnerSeat]].Nickname;
            //characterImage.sprite = scoreData.characterSprite;

            DisplayYakuScores(yakuOrigin.GetComponent<RectTransform>(), yakuObjectPrefab, scoreData.yaku_score_list);

            // 확인 버튼 이벤트
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(() => Destroy(gameObject)); // 팝업 닫기
        }

        public string ConvertHandTilesToString(List<GameTile> handTiles)
        {
            if (handTiles == null || handTiles.Count == 0)
            {
                return "Empty hand";
            }
            // Sort tiles by their numeric value
            var sortedTiles = handTiles.OrderBy(tile => (int)tile).ToList();
            StringBuilder sb = new StringBuilder();
            string currentPrefix = "";
            foreach (GameTile tile in sortedTiles)
            {
                string tileName = Enum.GetName(typeof(GameTile), tile);
                if (string.IsNullOrEmpty(tileName))
                {
                    continue;
                }
                string prefix = tileName.Substring(0, 1); // Get first character (M/P/S/Z/F)
                string value = tileName.Substring(1);    // Get remaining characters
                if (currentPrefix != prefix)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(prefix);
                    currentPrefix = prefix;
                }
                sb.Append(value);
            }
            return sb.ToString();
        }
        public void DisplayYakuScores(RectTransform panel, GameObject yakuObjectPrefab, List<YakuScore> yakuScores)
        {
            // 기존 항목 정리
            foreach (Transform child in panel)
            {
                Destroy(child.gameObject);
            }

            if (panel == null || yakuObjectPrefab == null)
            {
                Debug.LogError("Panel or YakuItemPrefab is null!");
                return;
            }
            float startX = 50f;
            float startY = -50f;
            // 각 야쿠 점수 표시
            for (int i = 0; i < yakuScores.Count; i++)
            {
                string name = Enum.GetName(typeof(Yaku), yakuScores[i].YakuId) ?? "";
                string score = yakuScores[i].Score.ToString("N0");
                GameObject itemObj = Instantiate(yakuObjectPrefab, panel);
                float animationDuration = 0.5f;
                float delayMult = 0.8f;
                float panelWidth = yakuPanel.GetComponent<RectTransform>().rect.width;
                int nOfRows = yakuScores.Count > 10 ? 5 : 4;
                int nOfColumns = yakuScores.Count > 8 ? 3 : 2;
                float yakuScale = panelWidth / nOfColumns / 550f;
                float yakuWidth = 500f * yakuScale;
                float yakuHeight = 100f * yakuScale;
                if (itemObj.TryGetComponent<YakuObject>(out var item))
                {
                    item.SetYakuInfo(name, score);
                    item.transform.localScale = Vector3.one * yakuScale;
                    item.GetComponent<RectTransform>().anchoredPosition = 
                    new Vector2(-10f + yakuWidth * (i / nOfRows), startY - yakuHeight * (i % nOfRows));
                    item.GetComponent<RectTransform>().DOAnchorPosX(startX + yakuWidth * (i / nOfRows), animationDuration)
                        .SetDelay((i + 1) * delayMult)
                        .SetEase(Ease.OutBack);

                    // 투명도 애니메이션 (선택사항)
                    CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
                    if (canvasGroup == null) canvasGroup = item.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0;
                    if (i == yakuScores.Count - 1) canvasGroup.DOFade(1, animationDuration).SetDelay((i + 1) * delayMult).onComplete = DisplayScores;
                    else canvasGroup.DOFade(1, animationDuration).SetDelay((i + 1) * delayMult);
                }
            }
        }

        public static void DisplayScores()
        {
            Debug.Log("Score");
        }
    }
}

