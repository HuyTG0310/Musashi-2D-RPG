using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Musashi.Core;

namespace Musashi.UI
{
    public class GameEndManager : MonoBehaviour
    {
        public static GameEndManager Instance { get; private set; }

        public enum EndGameType
        {
            None,
            Victory,
            Defeat
        }

        [Header("Audio Settings")]
        [Tooltip("Victory fanfare sound clip.")]
        public AudioClip victoryClip;
        [Tooltip("Defeat / Game Over sound clip.")]
        public AudioClip defeatClip;
        [Tooltip("Button click sound clip.")]
        public AudioClip clickSound;

        [Header("Scene Settings")]
        public string mainMenuSceneName = "MainMenuScene";

        private AudioSource audioSource;
        private Canvas endCanvas;
        private Image fadeOverlay;
        private GameObject endModalPanel;

        // UI Text & References
        private Image watermarkImage;
        private Image modalCardBg;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;

        // Overview Badges Text
        private TextMeshProUGUI hpOverviewText;
        private TextMeshProUGUI coinOverviewText;
        private TextMeshProUGUI timeOverviewText;

        // Table Rows
        private class TableRowUI
        {
            public TextMeshProUGUI itemName;
            public TextMeshProUGUI obtained;
            public TextMeshProUGUI used;
            public TextMeshProUGUI remaining;

            public void SetValues(string name, int obt, int u, int rem)
            {
                if (itemName != null) itemName.text = name;
                if (obtained != null) obtained.text = obt.ToString();
                if (used != null) used.text = u.ToString();
                if (remaining != null) remaining.text = rem.ToString();
            }
        }

        private TableRowUI coinRow;
        private TableRowUI appleRow;
        private TableRowUI shurikenRow;
        private TableRowUI sakeRow;

        // Action Buttons
        private Button primaryActionButton;
        private TextMeshProUGUI primaryButtonText;
        private Button secondaryActionButton;
        private TextMeshProUGUI secondaryButtonText;

        private TMP_FontAsset fontAsset;
        private Sprite menuBgSprite;
        private Sprite buttonSprite;
        private Sprite victoryWatermarkSprite;
        private Sprite deathWatermarkSprite;

        public EndGameType CurrentState { get; private set; } = EndGameType.None;
        private bool isTriggered = false;

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
            LoadAssets();

            try
            {
                BuildUIHierarchy();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GameEndManager] Error building UI: " + ex);
            }
            finally
            {
                if (endModalPanel != null) endModalPanel.SetActive(false);
                if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            if (endModalPanel != null) endModalPanel.SetActive(false);
            if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isTriggered = false;
            CurrentState = EndGameType.None;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
            if (endModalPanel != null) endModalPanel.SetActive(false);
            if (endCanvas != null)
            {
                bool isMainMenu = string.Equals(scene.name, mainMenuSceneName, System.StringComparison.OrdinalIgnoreCase);
                endCanvas.enabled = !isMainMenu;
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // Pure 2D flat sound
                audioSource.ignoreListenerPause = true;
                audioSource.ignoreListenerVolume = false;
                audioSource.priority = 0; // Highest priority
            }

#if UNITY_EDITOR
            if (defeatClip == null)
            {
                defeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/defeat_music.mp3");
                if (defeatClip == null)
                    defeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Preview_UI_Sounds/defeat_melancholy_option2.mp3");
                if (defeatClip == null)
                    defeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/defeat_sound.mp3");
                if (defeatClip == null)
                    defeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/defeat_sound.wav");
            }
            if (victoryClip == null)
            {
                victoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/victory_sound.mp3");
                if (victoryClip == null)
                    victoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Preview_UI_Sounds/victory_fanfare_option1.mp3");
            }
            if (clickSound == null)
            {
                clickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/mouse_click.mp3");
                if (clickSound == null)
                    clickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/mouse_click.mp3");
            }
#endif

            if (clickSound == null)
            {
                clickSound = Resources.Load<AudioClip>("Audio/mouse_click");
                if (clickSound == null) clickSound = Resources.Load<AudioClip>("Audio/SFX/mouse_click");
            }

            if (victoryClip == null)
            {
                victoryClip = Resources.Load<AudioClip>("Audio/victory_sound");
            }

            if (defeatClip == null)
            {
                defeatClip = Resources.Load<AudioClip>("Audio/defeat_music");
                if (defeatClip == null) defeatClip = Resources.Load<AudioClip>("Audio/defeat_sound");
            }

            if (victoryClip != null) victoryClip.LoadAudioData();
            if (defeatClip != null) defeatClip.LoadAudioData();
        }

        private void PlayEndMusic(AudioClip clip, bool loop = true)
        {
            EnsureAudioSource();
            if (clip == null)
            {
                Debug.LogWarning("[GameEndManager] End-game clip is missing!");
                return;
            }

            // Đồng bộ vị trí với AudioListener / Camera để không bao giờ bị tắt âm do khoảng cách
            if (Camera.main != null)
            {
                transform.position = Camera.main.transform.position;
            }
            else
            {
                AudioListener listener = FindObjectOfType<AudioListener>();
                if (listener != null)
                {
                    transform.position = listener.transform.position;
                }
            }

            AudioListener.pause = false;
            if (AudioListener.volume <= 0.05f)
            {
                AudioListener.volume = 1f;
            }

            audioSource.enabled = true;
            audioSource.mute = false;
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f;
            audioSource.loop = loop;
            audioSource.clip = clip;
            audioSource.Stop();
            audioSource.Play();
            Debug.Log($"[GameEndManager] Playing end music: {clip.name} (length: {clip.length:F1}s, loop: {loop})");
        }

        private void LoadAssets()
        {
#if UNITY_EDITOR
            if (fontAsset == null)
            {
                fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/PixelOperator8 SDF.asset");
                if (fontAsset == null)
                    fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Sprites/UI_Elements/PixelOperator8 SDF.asset");
            }
            if (menuBgSprite == null)
            {
                menuBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Elements/main_menu_bg.png");
            }
            if (buttonSprite == null)
            {
                buttonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Elements/wood_button.png");
            }
            if (victoryWatermarkSprite == null)
            {
                victoryWatermarkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Elements/kanji_victory_watermark.png");
            }
            if (deathWatermarkSprite == null)
            {
                deathWatermarkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Elements/kanji_death_watermark.png");
            }
#endif
            if (fontAsset == null)
            {
                fontAsset = Resources.Load<TMP_FontAsset>("Fonts/PixelOperator8 SDF");
            }

            if (menuBgSprite == null)
            {
                menuBgSprite = Resources.Load<Sprite>("UI/Sprites/main_menu_bg");
            }
            if (buttonSprite == null)
            {
                buttonSprite = Resources.Load<Sprite>("UI/Sprites/wood_button");
            }
            if (victoryWatermarkSprite == null)
            {
                victoryWatermarkSprite = Resources.Load<Sprite>("UI/Sprites/kanji_victory_watermark");
            }
            if (deathWatermarkSprite == null)
            {
                deathWatermarkSprite = Resources.Load<Sprite>("UI/Sprites/kanji_death_watermark");
            }
        }

        public void PlayClickSound()
        {
            EnsureAudioSource();
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        #region --- TRIGGER SEQUENCES ---

        /// <summary>
        /// Kích hoạt chuỗi Chiến thắng khi Boss cuối tử trận:
        /// Chậm thời gian (Time.timeScale = 0.2f) trong 2 giây để tôn vinh nhát chém quyết định,
        /// sau đó hiển thị bảng UI Victory toàn màn hình cùng nhạc chiến thắng.
        /// </summary>
        public void TriggerVictory()
        {
            if (isTriggered) return;
            isTriggered = true;
            CurrentState = EndGameType.Victory;

            StartCoroutine(VictorySequenceRoutine());
        }

        private IEnumerator VictorySequenceRoutine()
        {
            // 1. Slow-motion 0.2f trong 2 giây tôn vinh nhát chém cuối của Musashi
            Time.timeScale = 0.2f;
            yield return new WaitForSecondsRealtime(2.0f);

            // 2. Dừng thời gian và tắt BGM cũ
            Time.timeScale = 0f;
            SilenceBackgroundMusic();

            // 3. Hiển thị UI Chiến thắng toàn màn hình sáng rõ như Main Menu
            PopulateVictoryData();
            if (endModalPanel != null)
            {
                endModalPanel.SetActive(true);
            }

            // 4. Phát âm thanh / bản nhạc chiến thắng
            PlayEndMusic(victoryClip, loop: true);
        }

        /// <summary>
        /// Kích hoạt chuỗi Thất bại khi Musashi hết máu:
        /// Tắt BGM đột ngột, đợi hoạt ảnh ngã gục, sau đó hiển thị màn hình YOU DIED toàn màn hình sáng rõ cùng nhạc Game Over.
        /// </summary>
        public void TriggerDefeat()
        {
            if (isTriggered) return;
            isTriggered = true;
            CurrentState = EndGameType.Defeat;

            StartCoroutine(DefeatSequenceRoutine());
        }

        private IEnumerator DefeatSequenceRoutine()
        {
            // 1. Tắt lịm toàn bộ nhạc nền màn chơi đột ngột
            SilenceBackgroundMusic();

            // 2. Chờ Musashi diễn xong hoạt ảnh ngã gục (1.2 giây)
            yield return new WaitForSecondsRealtime(1.2f);
            Time.timeScale = 0f;

            // 3. Hiển thị UI Thất bại toàn màn hình sáng rõ như Main Menu
            PopulateDefeatData();
            if (endModalPanel != null)
            {
                endModalPanel.SetActive(true);
            }

            // 4. Bắt đầu phát bản nhạc Thất bại / Game Over (looping để người chơi nghe suốt màn hình kết thúc)
            PlayEndMusic(defeatClip, loop: true);
        }

        private void SilenceBackgroundMusic()
        {
            AudioSource[] sources = FindObjectsOfType<AudioSource>();
            foreach (var src in sources)
            {
                if (src != audioSource)
                {
                    src.Stop();
                }
            }
        }

        #endregion

        #region --- DATA POPULATION ---

        private void PopulateVictoryData()
        {
            Player p = Player.instance;
            float maxHp = p != null ? p.maxHealth : 100f;
            float curHp = p != null ? Mathf.Max(0, p.currentHealth) : 0f;
            int coins = p != null ? p.coins : 0;

            if (titleText != null)
            {
                titleText.text = "VICTORY";
                titleText.color = new Color(1f, 0.88f, 0.25f, 1f); // Hoàng kim rực rỡ
            }

            if (subtitleText != null)
            {
                subtitleText.text = "Honoring the Final Slash";
                subtitleText.color = new Color(0.95f, 0.90f, 0.80f, 0.95f);
            }

            if (watermarkImage != null)
            {
                watermarkImage.sprite = victoryWatermarkSprite;
                watermarkImage.color = new Color(1f, 0.85f, 0.3f, 0.20f); // Golden Calligraphy Sumi-e
                watermarkImage.gameObject.SetActive(victoryWatermarkSprite != null);
            }

            if (modalCardBg != null)
            {
                modalCardBg.color = Color.white; // Giữ 100% màu sắc và độ sáng của hình nền menu
            }

            if (hpOverviewText != null) hpOverviewText.text = $"HP: {Mathf.RoundToInt(curHp)} / {Mathf.RoundToInt(maxHp)}";
            if (coinOverviewText != null) coinOverviewText.text = $"COINS: {coins}";
            if (timeOverviewText != null) timeOverviewText.text = $"TIME: {GameSessionTracker.GetFormattedTime()}";

            PopulateItemTable(p);

            if (primaryButtonText != null) primaryButtonText.text = "PLAY AGAIN";
            if (secondaryButtonText != null) secondaryButtonText.text = "MAIN MENU";
        }

        private void PopulateDefeatData()
        {
            Player p = Player.instance;
            float maxHp = p != null ? p.maxHealth : 100f;
            int coins = p != null ? p.coins : 0;

            if (titleText != null)
            {
                titleText.text = "YOU DIED";
                titleText.color = new Color(0.95f, 0.15f, 0.15f, 1f); // Đỏ thẫm nổi bật
            }

            if (subtitleText != null)
            {
                subtitleText.text = "Rise again, Musashi...";
                subtitleText.color = new Color(0.90f, 0.82f, 0.80f, 0.90f);
            }

            if (watermarkImage != null)
            {
                watermarkImage.sprite = deathWatermarkSprite;
                watermarkImage.color = new Color(0.90f, 0.10f, 0.10f, 0.22f); // Thư pháp chữ Tử đỏ sẫm
                watermarkImage.gameObject.SetActive(deathWatermarkSprite != null);
            }

            if (modalCardBg != null)
            {
                modalCardBg.color = Color.white; // Giữ 100% màu sắc và độ sáng của hình nền menu
            }

            if (hpOverviewText != null) hpOverviewText.text = $"HP: 0 / {Mathf.RoundToInt(maxHp)}";
            if (coinOverviewText != null) coinOverviewText.text = $"COINS: {coins}";
            if (timeOverviewText != null) timeOverviewText.text = $"SURVIVED: {GameSessionTracker.GetFormattedTime()}";

            PopulateItemTable(p);

            if (primaryButtonText != null) primaryButtonText.text = "RETRY";
            if (secondaryButtonText != null) secondaryButtonText.text = "MAIN MENU";
        }

        private void PopulateItemTable(Player p)
        {
            int curCoins = p != null ? p.coins : 0;
            int curApples = p != null ? p.appleCount : 0;
            int curShurikens = p != null ? p.shurikenCount : 0;
            int curSake = p != null ? p.sakeCount : 0;

            int coinsObtained = Mathf.Max(GameSessionTracker.CoinsObtained, curCoins + GameSessionTracker.CoinsSpent);
            int applesObtained = Mathf.Max(GameSessionTracker.ApplesObtained, curApples + GameSessionTracker.ApplesUsed);
            int shurikensObtained = Mathf.Max(GameSessionTracker.ShurikensObtained, curShurikens + GameSessionTracker.ShurikensUsed);
            int sakeObtained = Mathf.Max(GameSessionTracker.SakeObtained, curSake + GameSessionTracker.SakeUsed);

            if (coinRow != null)
                coinRow.SetValues("Coins", coinsObtained, GameSessionTracker.CoinsSpent, curCoins);

            if (appleRow != null)
                appleRow.SetValues("Apples", applesObtained, GameSessionTracker.ApplesUsed, curApples);

            if (shurikenRow != null)
                shurikenRow.SetValues("Shurikens", shurikensObtained, GameSessionTracker.ShurikensUsed, curShurikens);

            if (sakeRow != null)
                sakeRow.SetValues("Sake Gourd", sakeObtained, GameSessionTracker.SakeUsed, curSake);
        }

        #endregion

        #region --- BUTTON ACTIONS ---

        public void RestartCurrentLevel()
        {
            PlayClickSound();
            if (audioSource != null) audioSource.Stop();
            Time.timeScale = 1f;

            if (Player.instance != null)
            {
                Destroy(Player.instance.gameObject);
                Player.instance = null;
            }

            GameSessionTracker.ResetSession();

            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        public void ReturnToMainMenu()
        {
            PlayClickSound();
            if (audioSource != null) audioSource.Stop();
            Time.timeScale = 1f;

            if (Player.instance != null)
            {
                Destroy(Player.instance.gameObject);
                Player.instance = null;
            }

            GameSessionTracker.ResetSession();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        #endregion

        #region --- RUNTIME UI GENERATION ---

        private void BuildUIHierarchy()
        {
            // 1. Root Canvas
            GameObject canvasObj = new GameObject("GameEndCanvas");
            canvasObj.transform.SetParent(transform);
            endCanvas = canvasObj.AddComponent<Canvas>();
            endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            endCanvas.sortingOrder = 999; // Hiển thị trên cùng tất cả UI khác

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 2. Fullscreen Fade Overlay (dùng khi chuyển cảnh)
            GameObject overlayObj = new GameObject("FadeOverlay");
            overlayObj.transform.SetParent(canvasObj.transform, false);
            fadeOverlay = overlayObj.AddComponent<Image>();
            fadeOverlay.color = new Color(0, 0, 0, 0);
            RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlayObj.SetActive(false);

            // 3. Toàn màn hình như Menu Game (Anchor 0..1, SizeDelta 0..0)
            endModalPanel = new GameObject("EndModalPanel");
            endModalPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform modalRect = endModalPanel.AddComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.sizeDelta = Vector2.zero;
            modalRect.anchoredPosition = Vector2.zero;

            modalCardBg = endModalPanel.AddComponent<Image>();
            if (menuBgSprite != null)
            {
                modalCardBg.sprite = menuBgSprite;
                modalCardBg.type = Image.Type.Simple;
            }
            modalCardBg.color = Color.white; // Sáng 100% nguyên bản như hình 2

            // 4. Watermark Japanese Calligraphy (Chữ Hán thư pháp mờ ở trung tâm)
            GameObject watermarkObj = new GameObject("WatermarkCalligraphy");
            watermarkObj.transform.SetParent(endModalPanel.transform, false);
            RectTransform wmRect = watermarkObj.AddComponent<RectTransform>();
            wmRect.sizeDelta = new Vector2(380, 380);
            wmRect.anchoredPosition = new Vector2(0, 50);
            watermarkImage = watermarkObj.AddComponent<Image>();
            watermarkImage.raycastTarget = false;
            watermarkObj.SetActive(false);

            // 5. Header Title Text ("VICTORY" / "YOU DIED")
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(endModalPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 360);
            titleRect.sizeDelta = new Vector2(900, 80);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            ConfigureText(titleText, 44, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.text = "YOU DIED";

            // 6. Subtitle Text ("Honoring the Final Slash" / "Rise again, Musashi...")
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(endModalPanel.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0, 305);
            subRect.sizeDelta = new Vector2(900, 36);
            subtitleText = subObj.AddComponent<TextMeshProUGUI>();
            ConfigureText(subtitleText, 16, FontStyles.Italic, TextAlignmentOptions.Center);
            subtitleText.text = "Rise again, Musashi...";

            // 7. General Stats Overview Badges (HP, Coins, Time / Survived)
            GameObject overviewObj = new GameObject("StatsOverviewBar");
            overviewObj.transform.SetParent(endModalPanel.transform, false);
            RectTransform overRect = overviewObj.AddComponent<RectTransform>();
            overRect.anchoredPosition = new Vector2(0, 220);
            overRect.sizeDelta = new Vector2(760, 46);

            Image overBg = overviewObj.AddComponent<Image>();
            overBg.color = new Color(0.10f, 0.08f, 0.07f, 0.85f); // Khung bán trong suốt cho số liệu nổi bật

            hpOverviewText = CreateStatBadge(overviewObj.transform, new Vector2(-240, 0), "HP: 100 / 100", Color.green);
            coinOverviewText = CreateStatBadge(overviewObj.transform, new Vector2(0, 0), "COINS: 0", new Color(1f, 0.85f, 0.2f));
            timeOverviewText = CreateStatBadge(overviewObj.transform, new Vector2(240, 0), "TIME: 00:00", new Color(0.4f, 0.8f, 1f));

            // 8. ITEMS SUMMARY TABLE (Bảng tổng kết vật phẩm chia thành các cột dọc)
            GameObject tableContainer = new GameObject("ItemsSummaryTable");
            tableContainer.transform.SetParent(endModalPanel.transform, false);
            RectTransform tableRect = tableContainer.AddComponent<RectTransform>();
            tableRect.anchoredPosition = new Vector2(0, 50);
            tableRect.sizeDelta = new Vector2(760, 230);

            Image tableBg = tableContainer.AddComponent<Image>();
            tableBg.color = new Color(0.10f, 0.08f, 0.07f, 0.80f);

            // 8.1 Table Header Row (4 Vertical Columns)
            CreateHeaderColumns(tableContainer.transform, new Vector2(0, 85));

            // 8.2 Rows for Coin, Apple, Shuriken, Sake (4 Vertical Columns Each)
            coinRow = CreateTableColumnRow(tableContainer.transform, new Vector2(0, 48), new Color(0.18f, 0.15f, 0.13f, 0.55f), "Coins");
            appleRow = CreateTableColumnRow(tableContainer.transform, new Vector2(0, 16), new Color(0.14f, 0.12f, 0.11f, 0.55f), "Apples");
            shurikenRow = CreateTableColumnRow(tableContainer.transform, new Vector2(0, -16), new Color(0.18f, 0.15f, 0.13f, 0.55f), "Shurikens");
            sakeRow = CreateTableColumnRow(tableContainer.transform, new Vector2(0, -48), new Color(0.14f, 0.12f, 0.11f, 0.55f), "Sake Gourd");

            // 9. Action Buttons Row (PLAY AGAIN / RETRY and MAIN MENU)
            GameObject buttonContainer = new GameObject("ActionButtonsRow");
            buttonContainer.transform.SetParent(endModalPanel.transform, false);
            RectTransform btnRowRect = buttonContainer.AddComponent<RectTransform>();
            btnRowRect.anchoredPosition = new Vector2(0, -120);
            btnRowRect.sizeDelta = new Vector2(760, 65);

            // Primary Action Button (Play Again / Retry)
            primaryActionButton = CreateActionButton(buttonContainer.transform, new Vector2(-160, 0), "RETRY", () => RestartCurrentLevel(), out primaryButtonText);

            // Secondary Action Button (Main Menu)
            secondaryActionButton = CreateActionButton(buttonContainer.transform, new Vector2(160, 0), "MAIN MENU", () => ReturnToMainMenu(), out secondaryButtonText);

            // Cuối cùng ẩn hoàn toàn modal panel khi bắt đầu game
            endModalPanel.SetActive(false);
        }

        private TextMeshProUGUI CreateStatBadge(Transform parent, Vector2 pos, string defaultText, Color textColor)
        {
            GameObject obj = new GameObject("StatBadge");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(230, 38);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            ConfigureText(tmp, 13, FontStyles.Bold, TextAlignmentOptions.Center);
            tmp.color = textColor;
            tmp.text = defaultText;
            return tmp;
        }

        private void CreateHeaderColumns(Transform parent, Vector2 pos)
        {
            GameObject headerObj = new GameObject("TableHeader");
            headerObj.transform.SetParent(parent, false);
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchoredPosition = pos;
            headerRect.sizeDelta = new Vector2(730, 34);

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.24f, 0.17f, 0.12f, 0.92f);

            CreateCellText(headerObj.transform, new Vector2(-220, 0), new Vector2(240, 30), "ITEM", FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.6f));
            CreateCellText(headerObj.transform, new Vector2(-40, 0), new Vector2(120, 30), "OBTAINED", FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.6f));
            CreateCellText(headerObj.transform, new Vector2(90, 0), new Vector2(120, 30), "USED", FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.6f));
            CreateCellText(headerObj.transform, new Vector2(220, 0), new Vector2(120, 30), "REMAINING", FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.6f));
        }

        private TableRowUI CreateTableColumnRow(Transform parent, Vector2 pos, Color rowBgColor, string defaultItemName)
        {
            GameObject rowObj = new GameObject("Row_" + defaultItemName);
            rowObj.transform.SetParent(parent, false);
            RectTransform rect = rowObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(730, 28);

            Image bg = rowObj.AddComponent<Image>();
            bg.color = rowBgColor;

            TableRowUI row = new TableRowUI();
            row.itemName = CreateCellText(rowObj.transform, new Vector2(-220, 0), new Vector2(240, 26), defaultItemName, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
            row.obtained = CreateCellText(rowObj.transform, new Vector2(-40, 0), new Vector2(120, 26), "0", FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
            row.used = CreateCellText(rowObj.transform, new Vector2(90, 0), new Vector2(120, 26), "0", FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
            row.remaining = CreateCellText(rowObj.transform, new Vector2(220, 0), new Vector2(120, 26), "0", FontStyles.Normal, TextAlignmentOptions.Center, Color.white);

            return row;
        }

        private TextMeshProUGUI CreateCellText(Transform parent, Vector2 pos, Vector2 size, string text, FontStyles style, TextAlignmentOptions align, Color color)
        {
            GameObject cellObj = new GameObject("Cell");
            cellObj.transform.SetParent(parent, false);
            RectTransform rect = cellObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = cellObj.AddComponent<TextMeshProUGUI>();
            ConfigureText(tmp, 12, style, align);
            tmp.color = color;
            tmp.text = text;
            return tmp;
        }

        private Button CreateActionButton(Transform parent, Vector2 pos, string label, System.Action onClick, out TextMeshProUGUI labelTmp)
        {
            GameObject btnObj = new GameObject(label + "Btn");
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(260, 58);

            Image img = btnObj.AddComponent<Image>();
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = cb;

            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            labelTmp = textObj.AddComponent<TextMeshProUGUI>();
            ConfigureText(labelTmp, 14, FontStyles.Bold, TextAlignmentOptions.Center);
            labelTmp.color = Color.white;
            labelTmp.text = label;

            return btn;
        }

        private void ConfigureText(TextMeshProUGUI tmp, float size, FontStyles style, TextAlignmentOptions align)
        {
            if (tmp == null) return;
            try
            {
                if (fontAsset != null && fontAsset.material != null)
                {
                    tmp.font = fontAsset;
                }
            }
            catch
            {
                // Fallback nếu font chưa load kịp
            }

            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.raycastTarget = false;
        }

        #endregion

        #region --- AUTO INITIALIZATION ---

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

            if (Instance == null && Object.FindObjectOfType<GameEndManager>() == null)
            {
                GameObject managerObj = new GameObject("GameEndManager");
                managerObj.AddComponent<GameEndManager>();
            }
        }

        #endregion
    }
}
