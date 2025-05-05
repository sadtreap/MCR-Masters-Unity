using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MCRGame.Game
{
    public class NavigationPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private List<Button> navButtons;

        [Header("Labels")]
        [SerializeField] private List<string> collapsedLabels = new List<string> { "화", "후", "꽃", "버" };
        [SerializeField] private List<string> expandedLabels = new List<string> { "자동화료", "후로금지", "자동화패", "쯔모기리" };

        [Header("Animation Settings")]
        [SerializeField] private float collapsedWidth = 60f;
        [SerializeField] private float expandedWidth = 180f;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease animationEase = Ease.OutQuad;

        private bool isExpanded = false;

        void OnEnable()
        {
            var gm = GameManager.Instance;
            gm.OnAutoHuFlagChanged += UpdateButtonColors;
            gm.OnPreventCallFlagChanged += UpdateButtonColors;
            gm.OnAutoFlowerFlagChanged += UpdateButtonColors;
            gm.OnTsumogiriFlagChanged += UpdateButtonColors;
        }

        void OnDisable()
        {
            var gm = GameManager.Instance;
            gm.OnAutoHuFlagChanged -= UpdateButtonColors;
            gm.OnPreventCallFlagChanged -= UpdateButtonColors;
            gm.OnAutoFlowerFlagChanged -= UpdateButtonColors;
            gm.OnTsumogiriFlagChanged -= UpdateButtonColors;
        }


        void Start()
        {
            // 초기 레이아웃 & 라벨 & 색상 세팅
            panelRect.sizeDelta = new Vector2(collapsedWidth, panelRect.sizeDelta.y);
            UpdateButtonLabels(false);
            UpdateButtonColors();

            // 배경 클릭으로 패널 토글
            backgroundButton.onClick.AddListener(TogglePanel);

            // 버튼 클릭 시 플래그 토글 + 색상 업데이트
            navButtons[0].onClick.AddListener(() =>
            {
                GameManager.Instance.AutoHuFlag = !GameManager.Instance.AutoHuFlag;
                UpdateButtonColors();
            });
            navButtons[1].onClick.AddListener(() =>
            {
                GameManager.Instance.PreventCallFlag = !GameManager.Instance.PreventCallFlag;
                UpdateButtonColors();
            });
            navButtons[2].onClick.AddListener(() =>
            {
                GameManager.Instance.AutoFlowerFlag = !GameManager.Instance.AutoFlowerFlag;
                UpdateButtonColors();
            });
            navButtons[3].onClick.AddListener(() =>
            {
                GameManager.Instance.TsumogiriFlag = !GameManager.Instance.TsumogiriFlag;
                UpdateButtonColors();
            });
        }

        private void TogglePanel()
        {
            // 1) 토글 후의 상태를 미리 계산
            bool expanding = !isExpanded;

            // 2) 목표 너비를 새로운 상태에 맞춰 결정
            float targetWidth = expanding ? expandedWidth : collapsedWidth;
            if (!expanding)
            {
                UpdateButtonLabels(expanding);
            }
            // 3) 애니메이션 실행, 완료 시 라벨과 색상 업데이트
            panelRect
                .DOSizeDelta(new Vector2(targetWidth, panelRect.sizeDelta.y), animationDuration)
                .SetEase(animationEase)
                .OnComplete(() =>
                {
                    UpdateButtonLabels(expanding);
                    UpdateButtonColors();
                });

            // 4) 내부 상태 업데이트
            isExpanded = expanding;
        }


        private void UpdateButtonLabels(bool expanded)
        {
            var labels = expanded ? expandedLabels : collapsedLabels;
            for (int i = 0; i < navButtons.Count; i++)
            {
                var txt = navButtons[i].GetComponentInChildren<Text>();
                if (txt != null)
                    txt.text = labels[i];
            }
        }

        private void UpdateButtonColors(bool _ = false)
        {
            // GameManager 값에 맞춰 텍스트 컬러만 변경
            SetButtonColor(navButtons[0], GameManager.Instance.AutoHuFlag);
            SetButtonColor(navButtons[1], GameManager.Instance.PreventCallFlag);
            SetButtonColor(navButtons[2], GameManager.Instance.AutoFlowerFlag);
            SetButtonColor(navButtons[3], GameManager.Instance.TsumogiriFlag);
        }
        private void SetButtonColor(Button btn, bool isOn)
        {
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
                txt.color = isOn ? Color.yellow : Color.white;
        }
    }
}
