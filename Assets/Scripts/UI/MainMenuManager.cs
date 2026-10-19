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

        [Header("Scene Names")]
        [SerializeField] private string defaultFirstMap = "Map1_AutumnForest";

        private void Start()
        {
            Time.timeScale = 1f;
            ShowMainPanel();
        }

        public void PlayGame()
        {
            SceneManager.LoadScene(defaultFirstMap);
        }

        public void LoadMap(string mapName)
        {
            SceneManager.LoadScene(mapName);
        }

        public void ShowMainPanel()
        {
            HideAllPanels();
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        public void ShowStory()
        {
            HideAllPanels();
            if (storyPanel != null) storyPanel.SetActive(true);
        }

        public void ShowHowToPlay()
        {
            HideAllPanels();
            if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
        }

        public void ShowSettings()
        {
            HideAllPanels();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void ShowAboutUs()
        {
            HideAllPanels();
            if (aboutUsPanel != null) aboutUsPanel.SetActive(true);
        }

        public void ShowLevelSelect()
        {
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
            Debug.Log("Game Exited!");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
