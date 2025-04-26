using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace MCRGame.UI
{
    public class BlinkTurnEffect : MonoBehaviour
    {
        private Image targetImage;
        private Coroutine blinkCoroutine;
        private float blinkDuration = 1f;
        private float maxAlpha = 1f;
        private float minAlpha = 0f;

        void Awake()
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null)
                Debug.LogWarning("BlinkTurnEffect: Image 컴포넌트가 없습니다.");
        }

        public void BlinkEffectOn()
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkEffect());
        }
        public void BlinkEffectOff()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            SetAlpha(0f); // ← 전체 투명 처리
        }

        public void SetAlpha(float alpha)
        {
            if (targetImage != null)
            {
                var color = targetImage.color;
                color.a = alpha;
                targetImage.color = color;
            }
        }


        private IEnumerator BlinkEffect()
        {
            while (true)
            {
                for (float t = 0; t < blinkDuration; t += Time.deltaTime)
                {
                    float alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PingPong(t, blinkDuration / 2f) / (blinkDuration / 2f));
                    if (targetImage != null)
                    {
                        var color = targetImage.color;
                        color.a = alpha;
                        targetImage.color = color;
                    }
                    yield return null;
                }
            }
        }
    }
}
