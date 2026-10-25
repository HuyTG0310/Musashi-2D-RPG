using UnityEngine;
using UnityEngine.SceneManagement;

namespace Musashi.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject aboutUsPanel;
        [SerializeField] private GameObject levelSelectPanel;

        [Header("Audio Settings")]
        [Tooltip("AudioSource dành riêng cho Nhạc nền (BGM). Không dùng để phát âm thanh hiệu ứng (SFX).")]
        [SerializeField] private AudioSource bgmAudioSource;
        [Tooltip("AudioSource dành riêng cho Hiệu ứng âm thanh (SFX) click chuột.")]
        [SerializeField] private AudioSource sfxAudioSource;
        [Tooltip("Âm thanh click chuột cho các nút giao diện.")]
        [SerializeField] private AudioClip buttonClickClip;

        [Header("Scene Names")]
        [SerializeField] private string defaultFirstMap = "Map1_AutumnForest";

        private void Awake()
        {
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            // 1. Tìm hoặc tách riêng AudioSource cho BGM (nhạc nền)
            if (bgmAudioSource == null)
            {
                var menuAudioObj = GameObject.Find("MenuAudio");
                if (menuAudioObj != null)
                {
                    bgmAudioSource = menuAudioObj.GetComponent<AudioSource>();
                }
            }

            // 2. Tìm hoặc tạo mới AudioSource cho SFX (click chuột), tách biệt hoàn toàn khỏi BGM
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
                if (sfxAudioSource == null)
                {
                    sfxAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (sfxAudioSource != null)
            {
                sfxAudioSource.playOnAwake = false;
                sfxAudioSource.loop = false;
                sfxAudioSource.spatialBlend = 0f;
                sfxAudioSource.ignoreListenerPause = true;
            }

            // 3. Tải âm thanh click chuột nếu chưa được gán
            if (buttonClickClip == null)
            {
                buttonClickClip = Resources.Load<AudioClip>("Audio/mouse_click");
                if (buttonClickClip == null)
                {
                    buttonClickClip = Resources.Load<AudioClip>("Audio/SFX/mouse_click");
                }
            }
        }

        /// <summary>
        /// Phát âm thanh click chuột trên kênh SFX riêng biệt, không đè hay ảnh hưởng đến nhạc nền.
        /// </summary>
        public void PlayClickSound()
        {
            EnsureAudioSources();
            if (sfxAudioSource != null && buttonClickClip != null)
            {
                sfxAudioSource.PlayOneShot(buttonClickClip);
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            // Khởi tạo hiển thị panel chính mà không phát tiếng click
            HideAllPanels();
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        public void PlayGame()
        {
            PlayClickSound();

            // Dừng nhạc nền menu sạch sẽ trước khi chuyển vào game
            if (bgmAudioSource != null)
            {
                bgmAudioSource.Stop();
            }

            if (Player.instance != null)
            {
                Destroy(Player.instance.gameObject);
                Player.instance = null;
            }
            SceneManager.LoadScene(defaultFirstMap);
        }

        public void LoadMap(string mapName)
        {
            PlayClickSound();

            if (bgmAudioSource != null)
            {
                bgmAudioSource.Stop();
            }

            if (Player.instance != null)
            {
                Destroy(Player.instance.gameObject);
                Player.instance = null;
            }
            SceneManager.LoadScene(mapName);
        }

        public void ShowMainPanel()
        {
            PlayClickSound();
            HideAllPanels();
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        public void ShowStory()
        {
            PlayClickSound();
            HideAllPanels();
            if (storyPanel != null) storyPanel.SetActive(true);
        }

        public void ShowHowToPlay()
        {
            PlayClickSound();
            HideAllPanels();
            if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
        }

        public void ShowSettings()
        {
            PlayClickSound();
            HideAllPanels();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void ShowAboutUs()
        {
            PlayClickSound();
            HideAllPanels();
            if (aboutUsPanel != null) aboutUsPanel.SetActive(true);
        }

        public void ShowLevelSelect()
        {
            PlayClickSound();
            HideAllPanels();
            if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        }

        private void HideAllPanels()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (aboutUsPanel != null) aboutUsPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        }

        public void QuitGame()
        {
            PlayClickSound();
            Debug.Log("Game Exited!");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
