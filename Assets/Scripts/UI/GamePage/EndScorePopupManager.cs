using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MCRGame.Common;
using MCRGame.Game;

namespace MCRGame.UI
{
    public class EndScorePopupManager : MonoBehaviour
    {
        [Header("Popup Container (직접 위치 조정)")]
        [SerializeField] private RectTransform popupContainer;

        [Header("Entry Prefab")]
        [SerializeField] private GameObject playerEntryPrefab;

        [Header("Horizontal Offsets")]
        [Tooltip("애니메이션 시작 X 위치 (왼쪽)")]
        private float startXOffset = -200f;
        [Tooltip("애니메이션 종료 X 위치 (오른쪽)")]
        private float targetXOffset = 650f;

        [Header("Animation Settings")]
        private float entryDelay = 1f;
        [SerializeField] private float moveDuration = 0.5f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float firstPlaceDelay = 0.3f;
        [Header("First Place Pop Settings")]
        [Tooltip("1등이 팝업할 최대 스케일")]
        [SerializeField] private float firstPlacePopScale = 1.5f;
        [Tooltip("1등 팝 후 돌아올 최종 스케일")]
        [SerializeField] private float firstPlaceRestScale = 1.2f;
        [Tooltip("팝업 애니메이션 지속 시간")]
        [SerializeField] private float firstPlacePopDuration = 0.3f;
        [Tooltip("돌아올 때 애니메이션 지속 시간")]
        [SerializeField] private float firstPlaceRestDuration = 0.2f;


        [Header("Vertical Spacing")]
        [Tooltip("각 항목 사이의 Y 간격")]
        private float verticalSpacing = 150f;
        private float defaultYpos = 200f;


        [Header("Overshoot Settings")]
        [Tooltip("목표 위치에서 얼마나 더 나아갈지(픽셀)")]
        [SerializeField] private float overshootDistance = 30f;
        [Tooltip("Overshoot 단계 비율 (0~1)")]
        [SerializeField][Range(0f, 1f)] private float overshootPhase = 0.7f;

        [Header("Ordinal Colors")]
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);    // #FFD700
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f); // #C0C0C0
        [SerializeField] private Color bronzeColor = new Color(0.80f, 0.50f, 0.20f); // #CD7F32
        [SerializeField] private Color grayColor = Color.gray;


        [Header("First Place Border")]
        [Tooltip("1등 항목에 추가할 Outline 두께")]
        [SerializeField] private Vector2 firstPlaceOutlineDistance = new Vector2(4, 4);

        private readonly List<GameObject> entries = new List<GameObject>();


        public IEnumerator ShowScores(IEnumerable<Player> players)
        {
            ClearEntries();
            var sorted = players
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Index)
                .ToList();
            int count = sorted.Count;

            for (int rank = count; rank >= 1; rank--)
            {
                int idx = rank - 1;
                var player = sorted[idx];

                // 1) Instantiate
                var entry = Instantiate(playerEntryPrefab, popupContainer);

                // 1.1) 등수별 크기(scale) 적용
                float scaleFactor = rank switch
                {
                    4 => 0.7f,
                    3 => 0.8f,
                    2 => 0.9f,
                    1 => 1.0f,
                    _ => 1.0f,
                };
                entry.transform.localScale = Vector3.one * scaleFactor;

                // 2) pivot을 오른쪽 중앙으로 설정
                var rt = entry.GetComponent<RectTransform>();
                rt.pivot = new Vector2(1f, 0.5f);

                // 3) 등수 전용 Text 설정
                var rankText = entry.transform.Find("Rank").GetComponentInChildren<Text>();
                rankText.text = rank.ToString();
                // 등수별 색상 적용
                Color prefixColor = rank switch
                {
                    1 => goldColor,
                    2 => silverColor,
                    3 => bronzeColor,
                    _ => grayColor
                };
                rankText.color = prefixColor;

                // 4) 이름
                var nameText = entry.transform.Find("NickName").GetComponentInChildren<Text>();
                nameText.text = player.Nickname;

                // 5) 점수 텍스트 & 색상
                var scoreText = entry.transform.Find("Score").GetComponentInChildren<Text>();
                scoreText.text = player.Score.ToString();
                if (player.Score > 0) scoreText.color = GameManager.Instance.PositiveScoreColor;
                else if (player.Score < 0) scoreText.color = GameManager.Instance.NegativeScoreColor;
                else scoreText.color = GameManager.Instance.ZeroScoreColor;

                // 6) 1등 테두리
                if (rank == 1)
                {
                    var outline = entry.GetComponent<Outline>() ?? entry.AddComponent<Outline>();
                    outline.effectColor = goldColor;
                    outline.effectDistance = firstPlaceOutlineDistance;
                }

                // 7) 애니메이션 세팅
                float y = defaultYpos - idx * verticalSpacing;
                rt.anchoredPosition = new Vector2(startXOffset, y);

                var cg = entry.GetComponent<CanvasGroup>() ?? entry.AddComponent<CanvasGroup>();
                cg.alpha = 0f;

                StartCoroutine(AnimateEntry(
                    entry,
                    from: new Vector2(startXOffset, y),
                    to: new Vector2(targetXOffset, y),
                    delay: 0f));

                if (rank == 1)
                {
                    float popDelay = firstPlaceDelay + moveDuration;
                    StartCoroutine(DelayedPop(entry, popDelay));
                }

                yield return new WaitForSeconds(entryDelay);
            }
        }


        private IEnumerator AnimateEntry(GameObject entry, Vector2 from, Vector2 to, float delay)
        {
            yield return new WaitForSeconds(delay);

            var rt = entry.GetComponent<RectTransform>();
            var cg = entry.GetComponent<CanvasGroup>();

            // 1) overshoot 지점 계산
            Vector2 overshootPos = to + Vector2.right * overshootDistance;

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                // phase 구분
                if (t < overshootPhase)
                {
                    // 1단계: from → overshootPos
                    float t1 = t / overshootPhase;
                    float eased1 = moveCurve.Evaluate(t1);
                    rt.anchoredPosition = Vector2.LerpUnclamped(from, overshootPos, eased1);
                }
                else
                {
                    // 2단계: overshootPos → to
                    float t2 = (t - overshootPhase) / (1f - overshootPhase);
                    float eased2 = 1f - Mathf.Pow(1f - t2, 2); // ease-out quadratic
                    rt.anchoredPosition = Vector2.LerpUnclamped(overshootPos, to, eased2);
                }

                cg.alpha = t;
                yield return null;
            }

            // 최종 확정
            rt.anchoredPosition = to;
            cg.alpha = 1f;
        }
        private IEnumerator DelayedPop(GameObject entry, float delay)
        {
            yield return new WaitForSeconds(delay);

            var rt = entry.GetComponent<RectTransform>();
            Vector3 origScale = rt.localScale;

            // 1) 원본 로컬 높이 (스케일 적용 전)
            float H0 = rt.rect.height * origScale.y;
            // 2) 고정할 '바닥 Y 좌표' 계산 (피벗 위치에서 바닥까지 거리 = H0/2)
            float fixedBottomY = rt.anchoredPosition.y - (H0 / 2f);

            // ─── 1단계: 1.0 → firstPlacePopScale 팝업 ───
            float elapsed = 0f;
            while (elapsed < firstPlacePopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / firstPlacePopDuration);
                float popT = Mathf.Sin(t * Mathf.PI * 0.5f); // ease-out
                float scale = Mathf.Lerp(1f, firstPlacePopScale, popT);

                // 스케일 적용
                rt.localScale = origScale * scale;
                // 바닥을 고정시키기 위해, 바닥Y + (현재 높이/2)
                float centerY = fixedBottomY + (H0 * scale) / 2f;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, centerY);

                yield return null;
            }
            // 정확히 최대 스케일로 보정
            rt.localScale = origScale * firstPlacePopScale;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x,
                fixedBottomY + (H0 * firstPlacePopScale) / 2f);

            // ─── 2단계: firstPlacePopScale → firstPlaceRestScale 돌아오기 ───
            elapsed = 0f;
            while (elapsed < firstPlaceRestDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / firstPlaceRestDuration);
                // linear 또는 원하는 easing
                float scale = Mathf.Lerp(firstPlacePopScale, firstPlaceRestScale, t);

                rt.localScale = origScale * scale;
                float centerY2 = fixedBottomY + (H0 * scale) / 2f;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, centerY2);

                yield return null;
            }
            // 최종 보정
            rt.localScale = origScale * firstPlaceRestScale;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x,
                fixedBottomY + (H0 * firstPlaceRestScale) / 2f);
        }

        private void ClearEntries()
        {
            foreach (var e in entries) Destroy(e);
            entries.Clear();
        }
    }
}

