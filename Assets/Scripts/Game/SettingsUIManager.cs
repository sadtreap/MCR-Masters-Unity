using UnityEngine;
using UnityEngine.UI;
using MCRGame.Audio;
using MCRGame.Game;

namespace MCRgame.Game
{
    public class SettingsUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider actionVolumeSlider;
        [SerializeField] private Slider discardVolumeSlider;
        [SerializeField] private Toggle rightClickTsumogiriToggle;
        [SerializeField] private Button closeButton;

        private const string PREF_ACTION_VOL = "ActionVolume";
        private const string PREF_DISCARD_VOL = "DiscardVolume";
        private const string PREF_RIGHT_CLICK = "RightClickTsumogiri";

        void Start()
        {
            // 1) 버튼 콜백 연결
            settingsButton.onClick.AddListener(OpenPanel);
            closeButton.onClick.AddListener(ClosePanel);

            // 2) 인스펙터에서 드래그해 준 UI들 초기 상태 세팅
            settingsPanel.SetActive(false);

            // 3) 저장된 값 로드 (없으면 1.0 / false)
            float aVol = PlayerPrefs.GetFloat(PREF_ACTION_VOL, 1f);
            float dVol = PlayerPrefs.GetFloat(PREF_DISCARD_VOL, 1f);
            bool rightOn = PlayerPrefs.GetInt(PREF_RIGHT_CLICK, 0) == 1;

            actionVolumeSlider.value = aVol;
            discardVolumeSlider.value = dVol;
            rightClickTsumogiriToggle.isOn = rightOn;

            // 4) 슬라이더·토글 이벤트
            actionVolumeSlider.onValueChanged.AddListener(OnActionVolumeChanged);
            discardVolumeSlider.onValueChanged.AddListener(OnDiscardVolumeChanged);
            rightClickTsumogiriToggle.onValueChanged.AddListener(OnRightClickToggled);

            // 5) 적용
            ApplyActionVolume(aVol);
            ApplyDiscardVolume(dVol);
            GameManager.Instance.IsRightClickTsumogiri = rightOn;
        }

        private void OpenPanel() => settingsPanel.SetActive(true);
        private void ClosePanel() => settingsPanel.SetActive(false);

        private void OnActionVolumeChanged(float v)
        {
            ApplyActionVolume(v);
            PlayerPrefs.SetFloat(PREF_ACTION_VOL, v);
        }

        private void OnDiscardVolumeChanged(float v)
        {
            ApplyDiscardVolume(v);
            PlayerPrefs.SetFloat(PREF_DISCARD_VOL, v);
        }

        private void OnRightClickToggled(bool on)
        {
            GameManager.Instance.IsRightClickTsumogiri = on;
            PlayerPrefs.SetInt(PREF_RIGHT_CLICK, on ? 1 : 0);
        }

        private void ApplyActionVolume(float v)
        {
            if (ActionAudioManager.Instance != null)
                ActionAudioManager.Instance.Volume = v;
        }

        private void ApplyDiscardVolume(float v)
        {
            if (DiscardSoundManager.Instance != null)
                DiscardSoundManager.Instance.Volume = v;
        }
    }
}