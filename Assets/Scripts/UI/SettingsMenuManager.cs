using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Musashi.UI
{
    public class SettingsMenuManager : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI volumeValueText;

        [Header("Display Settings")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        private Resolution[] resolutions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeGlobalAudioVolume()
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            AudioListener.volume = Mathf.Clamp01(savedVolume);
        }

        private void Awake()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(SetVolume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(SetResolution);
            }
        }

        private void Start()
        {
            SetupResolutionDropdown();
            LoadSettings();
        }

        private void SetupResolutionDropdown()
        {
            if (resolutionDropdown == null) return;

            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        private void LoadSettings()
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = savedVolume;
            }
            SetVolume(savedVolume);

            if (fullscreenToggle != null)
            {
                bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
                fullscreenToggle.isOn = isFullscreen;
                SetFullscreen(isFullscreen);
            }
        }

        public void SetVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            AudioListener.volume = clamped;
            PlayerPrefs.SetFloat("MasterVolume", clamped);
            PlayerPrefs.Save();

            if (volumeValueText != null)
            {
                volumeValueText.text = Mathf.RoundToInt(clamped * 100f) + "%";
            }
        }

        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetResolution(int resolutionIndex)
        {
            if (resolutions == null || resolutionIndex >= resolutions.Length) return;
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
            PlayerPrefs.Save();
        }
    }
}
