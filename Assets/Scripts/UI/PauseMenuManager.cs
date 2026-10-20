using UnityEngine;
using UnityEngine.SceneManagement;

namespace Musashi.UI
{
    public class PauseMenuManager : MonoBehaviour
    {
        public static PauseMenuManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuUI;
        [SerializeField] private GameObject pauseButtonObj;
        [SerializeField] private GameObject settingsPanel;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickClip;

        [Header("Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        public static bool IsPaused { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSource();
            EnsureEventSystem();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                audioSource.ignoreListenerPause = true;
            }

            if (buttonClickClip == null)
            {
                buttonClickClip = Resources.Load<AudioClip>("Audio/mouse_click");
                if (buttonClickClip == null)
                {
                    buttonClickClip = Resources.Load<AudioClip>("Audio/SFX/mouse_click");
                }
            }
        }

        public void PlayClickSound()
        {
            EnsureAudioSource();
            if (audioSource != null && buttonClickClip != null)
            {
                audioSource.PlayOneShot(buttonClickClip);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void Start()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isMainMenu = string.Equals(currentScene, mainMenuSceneName, System.StringComparison.OrdinalIgnoreCase);

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = !isMainMenu;
            }

            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (pauseButtonObj != null) pauseButtonObj.SetActive(!isMainMenu);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            IsPaused = false;
            EnsureEventSystem();

            bool isMainMenu = string.Equals(scene.name, mainMenuSceneName, System.StringComparison.OrdinalIgnoreCase);

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = !isMainMenu;
            }

            if (isMainMenu)
            {
                if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
                if (pauseButtonObj != null) pauseButtonObj.SetActive(false);
                if (settingsPanel != null) settingsPanel.SetActive(false);
            }
            else
            {
                if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
                if (pauseButtonObj != null) pauseButtonObj.SetActive(true);
                if (settingsPanel != null) settingsPanel.SetActive(false);
            }
        }

        private void Update()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (string.Equals(currentScene, mainMenuSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (IsPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            PlayClickSound();
            if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
            if (pauseButtonObj != null) pauseButtonObj.SetActive(false);
            Time.timeScale = 0f;
            IsPaused = true;
        }

        public void Resume()
        {
            PlayClickSound();
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pauseButtonObj != null) pauseButtonObj.SetActive(true);
            Time.timeScale = 1f;
            IsPaused = false;
        }

        public void RestartLevel()
        {
            PlayClickSound();
            Time.timeScale = 1f;
            IsPaused = false;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (pauseButtonObj != null) pauseButtonObj.SetActive(true);

            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        public void OpenSettings()
        {
            PlayClickSound();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            PlayClickSound();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void LoadMainMenu()
        {
            PlayClickSound();
            Time.timeScale = 1f;
            IsPaused = false;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (pauseButtonObj != null) pauseButtonObj.SetActive(false);

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            SceneManager.sceneLoaded -= OnAnySceneLoaded;
            SceneManager.sceneLoaded += OnAnySceneLoaded;
            TrySpawn(SceneManager.GetActiveScene().name);
        }

        private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TrySpawn(scene.name);
        }

        private static void TrySpawn(string sceneName)
        {
            if (string.Equals(sceneName, "MainMenuScene", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Instance == null && Object.FindObjectOfType<PauseMenuManager>() == null)
            {
                GameObject prefab = Resources.Load<GameObject>("UI/PauseMenuPrefab");
                if (prefab != null)
                {
                    Instantiate(prefab);
                }
            }
        }
    }
}
