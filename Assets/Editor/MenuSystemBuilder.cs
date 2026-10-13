using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;
using System.IO;
using Musashi.UI;

namespace Musashi.EditorTools
{
    public class MenuSystemBuilder : EditorWindow
    {
        [InitializeOnLoadMethod]
        private static void CheckAutoBuild()
        {
            string flag = "Temp/MusashiBuildMenu.flag";
            if (File.Exists(flag))
            {
                try { File.Delete(flag); } catch { }
                EditorApplication.delayCall += () =>
                {
                    BuildMenuSystemInternal(false);
                };
            }
        }

        [MenuItem("Tools/Musashi/1. Build Game Menu")]
        public static void BuildMenuSystem()
        {
            BuildMenuSystemInternal(true);
        }

        public static void BuildMenuSystemInternal(bool showDialog)
        {
            // 1. Load Font assets & Button sprite
            TMP_FontAsset customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Sprites/UI_Elements/PixelOperator8 SDF.asset");
            TMP_FontAsset readableFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Elements/wood_button.png");

            // Ensure directory exists
            string uiSceneDir = "Assets/Scenes/UI_Scenes";
            if (!Directory.Exists(uiSceneDir))
            {
                Directory.CreateDirectory(uiSceneDir);
                AssetDatabase.Refresh();
            }

            string scenePath = uiSceneDir + "/MainMenuScene.unity";

            // 2. Create new Scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = new Color(0.12f, 0.1f, 0.15f, 1f);
            }

            // 3. Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 4. Pixel Art Background Image
            string bgPath = "Assets/Sprites/UI_Elements/main_menu_bg.png";
            if (!File.Exists(bgPath))
            {
                bgPath = "Assets/Sprites/UI_Elements/main_menu_bg.jpg";
            }
            TextureImporter bgImporter = AssetImporter.GetAtPath(bgPath) as TextureImporter;
            if (bgImporter != null)
            {
                bool dirty = false;
                if (bgImporter.textureType != TextureImporterType.Sprite)
                {
                    bgImporter.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (bgImporter.filterMode != FilterMode.Point)
                {
                    bgImporter.filterMode = FilterMode.Point;
                    dirty = true;
                }
                if (bgImporter.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    bgImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }
                if (dirty)
                {
                    bgImporter.SaveAndReimport();
                }
            }
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);

            GameObject bgObj = CreateUIElement("Background", canvasObj.transform);
            Image bgImage = bgObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.color = Color.white;
            }
            else
            {
                bgImage.color = new Color(0.08f, 0.07f, 0.12f, 0.95f);
            }
            SetFullStretch(bgObj.GetComponent<RectTransform>());

            // 5. MainMenuController
            GameObject controllerObj = new GameObject("MainMenuController");
            MainMenuManager mainMgr = controllerObj.AddComponent<MainMenuManager>();

            // --- 6. MainPanel ---
            GameObject mainPanel = CreateUIElement("MainPanel", canvasObj.transform);
            SetFullStretch(mainPanel.GetComponent<RectTransform>());

            // Title
            GameObject titleObj = CreateUIElement("TitleText", mainPanel.transform);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "MUSASHI 2D RPG";
            titleText.fontSize = 62;
            titleText.alignment = TextAlignmentOptions.Center;
            if (customFont != null) titleText.font = customFont;
            titleText.color = new Color(1f, 0.85f, 0.3f, 1f);
            SetRect(titleObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.84f), new Vector2(850, 110), Vector2.zero);

            // Container for buttons
            GameObject mainBtnContainer = CreateUIElement("ButtonContainer", mainPanel.transform);
            SetRect(mainBtnContainer.GetComponent<RectTransform>(), new Vector2(0.5f, 0.40f), new Vector2(440, 520), Vector2.zero);

            VerticalLayoutGroup vlg = mainBtnContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // 6 Requested Main Buttons: PLAY GAME, STORY, HOW TO PLAY, SETTINGS, ABOUT US, QUIT GAME
            Button playBtn = CreateStyledButton("PlayButton", mainBtnContainer.transform, "PLAY GAME", buttonSprite, customFont, 24, 400, 58);
            Button storyBtn = CreateStyledButton("StoryButton", mainBtnContainer.transform, "STORY", buttonSprite, customFont, 24, 400, 58);
            Button howToPlayBtn = CreateStyledButton("HowToPlayButton", mainBtnContainer.transform, "HOW TO PLAY", buttonSprite, customFont, 24, 400, 58);
            Button settingsBtn = CreateStyledButton("SettingsButton", mainBtnContainer.transform, "SETTINGS", buttonSprite, customFont, 24, 400, 58);
            Button aboutUsBtn = CreateStyledButton("AboutUsButton", mainBtnContainer.transform, "ABOUT US", buttonSprite, customFont, 24, 400, 58);
            Button quitBtn = CreateStyledButton("QuitButton", mainBtnContainer.transform, "QUIT GAME", buttonSprite, customFont, 24, 400, 58);

            UnityEventTools.AddPersistentListener(playBtn.onClick, mainMgr.PlayGame);
            UnityEventTools.AddPersistentListener(storyBtn.onClick, mainMgr.ShowStory);
            UnityEventTools.AddPersistentListener(howToPlayBtn.onClick, mainMgr.ShowHowToPlay);
            UnityEventTools.AddPersistentListener(settingsBtn.onClick, mainMgr.ShowSettings);
            UnityEventTools.AddPersistentListener(aboutUsBtn.onClick, mainMgr.ShowAboutUs);
            UnityEventTools.AddPersistentListener(quitBtn.onClick, mainMgr.QuitGame);

            // --- 7. StoryPanel ---
            GameObject storyPanel = CreateUIElement("StoryPanel", canvasObj.transform);
            SetFullStretch(storyPanel.GetComponent<RectTransform>());

            GameObject storyTitleObj = CreateUIElement("StoryTitleText", storyPanel.transform);
            TextMeshProUGUI storyTitle = storyTitleObj.AddComponent<TextMeshProUGUI>();
            storyTitle.text = "STORY";
            storyTitle.fontSize = 52;
            storyTitle.alignment = TextAlignmentOptions.Center;
            if (customFont != null) storyTitle.font = customFont;
            storyTitle.color = new Color(1f, 0.85f, 0.3f, 1f);
            SetRect(storyTitleObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.86f), new Vector2(800, 90), Vector2.zero);

            GameObject storyFrame = CreateUIElement("StoryFrame", storyPanel.transform);
            SetRect(storyFrame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), new Vector2(980, 560), Vector2.zero);
            Image storyFrameImg = storyFrame.AddComponent<Image>();
            storyFrameImg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            GameObject storyTextObj = CreateUIElement("StoryText", storyFrame.transform);
            SetFullStretch(storyTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI storyText = storyTextObj.AddComponent<TextMeshProUGUI>();
            storyText.margin = new Vector4(38, 30, 38, 30);
            storyText.fontSize = 21;
            storyText.lineSpacing = 16;
            storyText.alignment = TextAlignmentOptions.TopLeft;
            storyText.color = new Color(0.92f, 0.92f, 0.96f, 1f);
            if (readableFont != null) storyText.font = readableFont;
            else if (customFont != null) storyText.font = customFont;
            storyText.text =
                "Once the Shogun’s greatest protector, Musashi was a master of the katana and the lethal shuriken. But ten years ago, when dark forces ravaged the kingdom, he watched helplessly as his lord fell. Consumed by guilt, the veteran samurai exiled himself to the remote Red Maple Forest, drowning his past in sake.\n\n" +
                "Now, the creeping darkness has reached his final sanctuary. Watching his last drop of sake spill onto the tainted earth, Musashi's drunken haze vanishes. He dons his old uniform, secures his sake gourd and shurikens, and draws the Shogun’s katana—shaking off a decade of dust. The broken man is dead; the legendary warrior has returned to collect a ten-year blood debt.";

            Button backBtnStory = CreateStyledButton("BackButton", storyPanel.transform, "BACK", buttonSprite, customFont, 22, 280, 55);
            SetRect(backBtnStory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.10f), new Vector2(280, 55), Vector2.zero);
            UnityEventTools.AddPersistentListener(backBtnStory.onClick, mainMgr.ShowMainPanel);

            storyPanel.SetActive(false);

            // --- 8. AboutUsPanel ---
            GameObject aboutUsPanel = CreateUIElement("AboutUsPanel", canvasObj.transform);
            SetFullStretch(aboutUsPanel.GetComponent<RectTransform>());

            GameObject aboutTitleObj = CreateUIElement("AboutTitleText", aboutUsPanel.transform);
            TextMeshProUGUI aboutTitle = aboutTitleObj.AddComponent<TextMeshProUGUI>();
            aboutTitle.text = "ABOUT US";
            aboutTitle.fontSize = 52;
            aboutTitle.alignment = TextAlignmentOptions.Center;
            if (customFont != null) aboutTitle.font = customFont;
            aboutTitle.color = new Color(1f, 0.85f, 0.3f, 1f);
            SetRect(aboutTitleObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.86f), new Vector2(800, 90), Vector2.zero);

            GameObject aboutFrame = CreateUIElement("AboutFrame", aboutUsPanel.transform);
            SetRect(aboutFrame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), new Vector2(900, 540), Vector2.zero);
            Image aboutFrameImg = aboutFrame.AddComponent<Image>();
            aboutFrameImg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            GameObject aboutTextObj = CreateUIElement("AboutText", aboutFrame.transform);
            SetFullStretch(aboutTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI aboutText = aboutTextObj.AddComponent<TextMeshProUGUI>();
            aboutText.margin = new Vector4(50, 40, 50, 35);
            aboutText.fontSize = 23;
            aboutText.lineSpacing = 16;
            aboutText.alignment = TextAlignmentOptions.TopLeft;
            aboutText.color = Color.white;
            if (readableFont != null) aboutText.font = readableFont;
            else if (customFont != null) aboutText.font = customFont;
            aboutText.text =
                "<size=28><color=#FFD700><b>PROJECT MENTOR:</b></color></size>\n" +
                "    Quách Luyl Đa\n\n" +
                "<size=28><color=#FFD700><b>GROUP MEMBERS:</b></color></size>\n" +
                "    <color=#D0D0D0>CE191481</color><pos=280>Đoàn Minh Duy\n" +
                "    <color=#D0D0D0>CE200044</color><pos=280>Trần Gia Huy\n" +
                "    <color=#D0D0D0>CE191482</color><pos=280>Nguyễn Tài Lợi\n" +
                "    <color=#D0D0D0>CE190395</color><pos=280>Nguyễn Tấn Vinh";

            Button backBtnAbout = CreateStyledButton("BackButton", aboutUsPanel.transform, "BACK", buttonSprite, customFont, 22, 280, 55);
            SetRect(backBtnAbout.GetComponent<RectTransform>(), new Vector2(0.5f, 0.10f), new Vector2(280, 55), Vector2.zero);
            UnityEventTools.AddPersistentListener(backBtnAbout.onClick, mainMgr.ShowMainPanel);

            aboutUsPanel.SetActive(false);

            // --- 9. HowToPlayPanel ---
            GameObject howToPlayPanel = CreateUIElement("HowToPlayPanel", canvasObj.transform);
            SetFullStretch(howToPlayPanel.GetComponent<RectTransform>());

            GameObject htpTitleObj = CreateUIElement("HTPTitleText", howToPlayPanel.transform);
            TextMeshProUGUI htpTitle = htpTitleObj.AddComponent<TextMeshProUGUI>();
            htpTitle.text = "HOW TO PLAY";
            htpTitle.fontSize = 52;
            htpTitle.alignment = TextAlignmentOptions.Center;
            if (customFont != null) htpTitle.font = customFont;
            htpTitle.color = new Color(1f, 0.85f, 0.3f, 1f);
            SetRect(htpTitleObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.86f), new Vector2(800, 90), Vector2.zero);

            GameObject htpFrame = CreateUIElement("GuideFrame", howToPlayPanel.transform);
            SetRect(htpFrame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), new Vector2(900, 540), Vector2.zero);
            Image htpFrameImg = htpFrame.AddComponent<Image>();
            htpFrameImg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            GameObject htpTextObj = CreateUIElement("GuideText", htpFrame.transform);
            SetFullStretch(htpTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI htpText = htpTextObj.AddComponent<TextMeshProUGUI>();
            htpText.margin = new Vector4(40, 30, 40, 30);
            htpText.fontSize = 19;
            htpText.lineSpacing = 14;
            htpText.alignment = TextAlignmentOptions.TopLeft;
            htpText.color = Color.white;
            if (customFont != null) htpText.font = customFont;
            htpText.text =
                "<color=#FFD700>  [ A / D ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Move Left / Right</color>\n" +
                "<color=#FFD700>  [ Shift + Move ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Run</color>\n" +
                "<color=#FFD700>  [ Space ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Jump</color>\n" +
                "<color=#FFD700>  [ J ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Sword Attack (Combo x3)</color>\n" +
                "<color=#FFD700>  [ K ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Defend / Block</color>\n" +
                "<color=#FFD700>  [ U ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Throw Shuriken</color>\n" +
                "<color=#FFD700>  [ C ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Dash</color>\n" +
                "<color=#FFD700>  [ 1 ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Eat Apple (Heal HP)</color>\n" +
                "<color=#FFD700>  [ E ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Open Shop (Near NPC)</color>\n" +
                "<color=#FFD700>  [ ESC ]</color><pos=350><color=#888888>:</color>  <pos=385><color=#FFFFFF>Pause Game</color>";

            Button backBtnHTP = CreateStyledButton("BackButton", howToPlayPanel.transform, "BACK", buttonSprite, customFont, 22, 280, 55);
            SetRect(backBtnHTP.GetComponent<RectTransform>(), new Vector2(0.5f, 0.10f), new Vector2(280, 55), Vector2.zero);
            UnityEventTools.AddPersistentListener(backBtnHTP.onClick, mainMgr.ShowMainPanel);

            howToPlayPanel.SetActive(false);

            // --- 10. SettingsPanel ---
            GameObject settingsPanel = CreateUIElement("SettingsPanel", canvasObj.transform);
            SetFullStretch(settingsPanel.GetComponent<RectTransform>());

            SettingsMenuManager settingsMgr = settingsPanel.AddComponent<SettingsMenuManager>();

            GameObject settingsTitleObj = CreateUIElement("SettingsTitleText", settingsPanel.transform);
            TextMeshProUGUI settingsTitle = settingsTitleObj.AddComponent<TextMeshProUGUI>();
            settingsTitle.text = "SETTINGS";
            settingsTitle.fontSize = 52;
            settingsTitle.alignment = TextAlignmentOptions.Center;
            if (customFont != null) settingsTitle.font = customFont;
            settingsTitle.color = new Color(1f, 0.85f, 0.3f, 1f);
            SetRect(settingsTitleObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.86f), new Vector2(800, 90), Vector2.zero);

            GameObject settingsContainer = CreateUIElement("SettingsContainer", settingsPanel.transform);
            SetRect(settingsContainer.GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), new Vector2(640, 360), Vector2.zero);

            VerticalLayoutGroup vlgSet = settingsContainer.AddComponent<VerticalLayoutGroup>();
            vlgSet.spacing = 30;
            vlgSet.childAlignment = TextAnchor.MiddleCenter;
            vlgSet.childControlWidth = true;
            vlgSet.childControlHeight = false;

            // Volume Row
            GameObject volObj = CreateUIElement("VolumeRow", settingsContainer.transform);
            volObj.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 60);
            HorizontalLayoutGroup hlgVol = volObj.AddComponent<HorizontalLayoutGroup>();
            hlgVol.spacing = 15;
            hlgVol.childAlignment = TextAnchor.MiddleCenter;
            hlgVol.childControlWidth = false;
            hlgVol.childControlHeight = true;

            GameObject volLabelObj = CreateUIElement("VolumeLabel", volObj.transform);
            volLabelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 50);
            TextMeshProUGUI volLabel = volLabelObj.AddComponent<TextMeshProUGUI>();
            volLabel.text = "MASTER VOLUME:";
            volLabel.fontSize = 18;
            volLabel.alignment = TextAlignmentOptions.Left;
            if (customFont != null) volLabel.font = customFont;
            volLabel.color = Color.white;

            // Slider
            GameObject sliderObj = CreateUIElement("VolumeSlider", volObj.transform);
            sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 24);
            Slider volSlider = sliderObj.AddComponent<Slider>();
            volSlider.minValue = 0f;
            volSlider.maxValue = 1f;
            volSlider.value = 1f;

            GameObject sliderBg = CreateUIElement("Background", sliderObj.transform);
            SetFullStretch(sliderBg.GetComponent<RectTransform>());
            Image sliderBgImg = sliderBg.AddComponent<Image>();
            sliderBgImg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

            GameObject fillArea = CreateUIElement("Fill Area", sliderObj.transform);
            RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5, 0);
            fillAreaRT.offsetMax = new Vector2(-15, 0);

            GameObject fill = CreateUIElement("Fill", fillArea.transform);
            SetFullStretch(fill.GetComponent<RectTransform>());
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.95f, 0.75f, 0.2f, 1f);
            volSlider.fillRect = fill.GetComponent<RectTransform>();

            GameObject handleArea = CreateUIElement("Handle Slide Area", sliderObj.transform);
            RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10, 0);
            handleAreaRT.offsetMax = new Vector2(-10, 0);

            GameObject handle = CreateUIElement("Handle", handleArea.transform);
            RectTransform handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(22, 28);
            Image handleImg = handle.AddComponent<Image>();
            if (buttonSprite != null)
            {
                handleImg.sprite = buttonSprite;
                handleImg.type = Image.Type.Sliced;
            }
            else
            {
                handleImg.color = new Color(1f, 0.9f, 0.5f, 1f);
            }
            volSlider.handleRect = handleRT;
            volSlider.targetGraphic = handleImg;

            // Volume Value Text
            GameObject volValObj = CreateUIElement("VolumeValueText", volObj.transform);
            volValObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 50);
            TextMeshProUGUI volValText = volValObj.AddComponent<TextMeshProUGUI>();
            volValText.text = "100%";
            volValText.fontSize = 18;
            volValText.alignment = TextAlignmentOptions.Center;
            volValText.color = new Color(1f, 0.85f, 0.3f, 1f);
            if (customFont != null) volValText.font = customFont;

            UnityEventTools.AddPersistentListener(volSlider.onValueChanged, settingsMgr.SetVolume);

            // Fullscreen Toggle
            GameObject toggleObj = CreateUIElement("FullscreenToggle", settingsContainer.transform);
            toggleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 50);
            Toggle fsToggle = toggleObj.AddComponent<Toggle>();

            GameObject toggleLabelObj = CreateUIElement("Label", toggleObj.transform);
            TextMeshProUGUI toggleLabel = toggleLabelObj.AddComponent<TextMeshProUGUI>();
            toggleLabel.text = "FULLSCREEN MODE";
            toggleLabel.fontSize = 18;
            if (customFont != null) toggleLabel.font = customFont;
            SetRect(toggleLabelObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(300, 40), new Vector2(-50, 0));

            UnityEventTools.AddPersistentListener(fsToggle.onValueChanged, settingsMgr.SetFullscreen);

            // Back Button Settings
            Button backBtnSettings = CreateStyledButton("BackButton", settingsContainer.transform, "BACK", buttonSprite, customFont, 22, 280, 55);
            UnityEventTools.AddPersistentListener(backBtnSettings.onClick, mainMgr.ShowMainPanel);

            settingsPanel.SetActive(false);

            // --- 11. Connect Serialized Properties via SerializedObject ---
            SerializedObject soMain = new SerializedObject(mainMgr);
            soMain.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            soMain.FindProperty("storyPanel").objectReferenceValue = storyPanel;
            soMain.FindProperty("howToPlayPanel").objectReferenceValue = howToPlayPanel;
            soMain.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            soMain.FindProperty("aboutUsPanel").objectReferenceValue = aboutUsPanel;
            soMain.ApplyModifiedProperties();

            SerializedObject soSet = new SerializedObject(settingsMgr);
            soSet.FindProperty("masterVolumeSlider").objectReferenceValue = volSlider;
            soSet.FindProperty("volumeValueText").objectReferenceValue = volValText;
            soSet.FindProperty("fullscreenToggle").objectReferenceValue = fsToggle;
            soSet.ApplyModifiedProperties();

            // 12. Save Scene
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log("<color=green>[Musashi] MainMenuScene built successfully at: " + scenePath + "</color>");

            // 13. Auto Update Build Settings
            UpdateBuildSettings(scenePath);

            // 14. Create Pause Menu Prefab & Setup All Maps
            CreatePauseMenuPrefab(buttonSprite, customFont);
            SetupPauseMenuInAllMaps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Thành công!", "Menu Game đã được cập nhật thành công 100%:\n\n1. 6 Nút Menu: PLAY GAME, STORY, HOW TO PLAY, SETTINGS, ABOUT US, QUIT GAME\n2. STORY: Hiển thị cốt truyện Musashi & Shogun\n3. ABOUT US: Đầy đủ GVHD Quách Luyl Đa & 4 thành viên nhóm\n4. HOW TO PLAY: Dấu hai chấm (:) và mô tả đã được căn chỉnh thẳng hàng\n5. SETTINGS: Thanh chỉnh âm lượng hiển thị % và điều khiển âm lượng chuẩn xác!", "OK");
            }
        }

        [MenuItem("Tools/Musashi/2. Setup Pause Menu In All Maps")]
        public static void SetupPauseMenuInAllMapsMenu()
        {
            TMP_FontAsset customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Sprites/UI_Elements/PixelOperator8 SDF.asset");
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI_Elements/wood_button.png");

            CreatePauseMenuPrefab(buttonSprite, customFont);
            SetupPauseMenuInAllMaps();

            EditorUtility.DisplayDialog("Thành công!", "Đã cài đặt Nút Dừng (|| PAUSE) và Menu Tạm Dừng (Chơi tiếp, Chơi lại, Thoát) vào tất cả 4 Map game!\n\n- Map 1: Autumn Forest\n- Map 2: Oak Forest\n- Map 3: Cave\n- Map 4: Desert\n\nBạn có thể bấm nút trên màn hình hoặc phím ESC / P để dừng game bất cứ lúc nào!", "OK");
        }

        public static void SetupPauseMenuInAllMaps()
        {
            string[] mapPaths = new string[]
            {
                "Assets/Scenes/Maps/Map1_AutumnForest.unity",
                "Assets/Scenes/Maps/Map2_OakForest.unity",
                "Assets/Scenes/Maps/Map3_Cave.unity",
                "Assets/Scenes/Maps/Map4_Desert.unity"
            };

            GameObject pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PauseMenuPrefab.prefab");
            if (pausePrefab == null)
            {
                Debug.LogError("PauseMenuPrefab not found!");
                return;
            }

            string currentOpenScene = EditorSceneManager.GetActiveScene().path;

            foreach (string path in mapPaths)
            {
                if (!File.Exists(path)) continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                // 1. Ensure EventSystem
                if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject esObj = new GameObject("EventSystem");
                    esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Debug.Log("[Musashi] Added EventSystem to " + path);
                }

                // 2. Ensure PauseCanvas
                PauseMenuManager existingPause = Object.FindObjectOfType<PauseMenuManager>();
                if (existingPause == null)
                {
                    GameObject pauseInst = (GameObject)PrefabUtility.InstantiatePrefab(pausePrefab);
                    pauseInst.name = "PauseCanvas";
                    Debug.Log("[Musashi] Added PauseCanvas to " + path);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            // Restore previously opened scene
            if (!string.IsNullOrEmpty(currentOpenScene) && File.Exists(currentOpenScene))
            {
                EditorSceneManager.OpenScene(currentOpenScene, OpenSceneMode.Single);
            }
            else
            {
                EditorSceneManager.OpenScene("Assets/Scenes/UI_Scenes/MainMenuScene.unity", OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Musashi] Successfully setup Pause Menu and EventSystem in all maps!</color>");
        }

        private static void UpdateBuildSettings(string mainMenuPath)
        {
            string[] mapPaths = new string[]
            {
                mainMenuPath,
                "Assets/Scenes/Maps/Map1_AutumnForest.unity",
                "Assets/Scenes/Maps/Map2_OakForest.unity",
                "Assets/Scenes/Maps/Map3_Cave.unity",
                "Assets/Scenes/Maps/Map4_Desert.unity"
            };

            EditorBuildSettingsScene[] newBuildScenes = new EditorBuildSettingsScene[mapPaths.Length];

            for (int i = 0; i < mapPaths.Length; i++)
            {
                newBuildScenes[i] = new EditorBuildSettingsScene(mapPaths[i], true);
            }

            EditorBuildSettings.scenes = newBuildScenes;
            Debug.Log("<color=green>[Musashi] Build Settings updated with MainMenuScene as Index 0!</color>");
        }

        private static void CreatePauseMenuPrefab(Sprite buttonSprite, TMP_FontAsset font)
        {
            string prefabDir = "Assets/Prefabs/UI";
            string resDir = "Assets/Resources/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

            string prefabPath = prefabDir + "/PauseMenuPrefab.prefab";
            string resPrefabPath = resDir + "/PauseMenuPrefab.prefab";

            GameObject rootObj = new GameObject("PauseCanvas");
            Canvas canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            rootObj.AddComponent<GraphicRaycaster>();
            PauseMenuManager pauseMgr = rootObj.AddComponent<PauseMenuManager>();

            // 1. HUD Pause Button (Top-Right of screen)
            GameObject pauseBtnObj = CreateUIElement("HUDPauseButton", rootObj.transform);
            RectTransform pauseBtnRT = pauseBtnObj.GetComponent<RectTransform>();
            pauseBtnRT.anchorMin = new Vector2(1f, 1f);
            pauseBtnRT.anchorMax = new Vector2(1f, 1f);
            pauseBtnRT.pivot = new Vector2(1f, 1f);
            pauseBtnRT.sizeDelta = new Vector2(160, 50);
            pauseBtnRT.anchoredPosition = new Vector2(-30, -30);

            Image pauseBtnImg = pauseBtnObj.AddComponent<Image>();
            if (buttonSprite != null)
            {
                pauseBtnImg.sprite = buttonSprite;
                pauseBtnImg.type = Image.Type.Sliced;
            }
            else
            {
                pauseBtnImg.color = new Color(0.25f, 0.2f, 0.15f, 1f);
            }

            Button pauseBtn = pauseBtnObj.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(pauseBtn.onClick, pauseMgr.Pause);

            GameObject pauseBtnTextObj = CreateUIElement("Text", pauseBtnObj.transform);
            SetFullStretch(pauseBtnTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI pauseBtnText = pauseBtnTextObj.AddComponent<TextMeshProUGUI>();
            pauseBtnText.text = "|| PAUSE";
            pauseBtnText.fontSize = 20;
            pauseBtnText.alignment = TextAlignmentOptions.Center;
            pauseBtnText.color = Color.white;
            if (font != null) pauseBtnText.font = font;

            // 2. Pause Menu Overlay Panel
            GameObject pausePanel = CreateUIElement("PauseMenuPanel", rootObj.transform);
            SetFullStretch(pausePanel.GetComponent<RectTransform>());
            Image bg = pausePanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);

            GameObject titleObj = CreateUIElement("PauseTitle", pausePanel.transform);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "GAME PAUSED";
            titleText.fontSize = 54;
            titleText.alignment = TextAlignmentOptions.Center;
            if (font != null) titleText.font = font;
            titleText.color = new Color(1f, 0.85f, 0.3f, 1f);
            SetRect(titleObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.78f), new Vector2(600, 100), Vector2.zero);

            GameObject btnContainer = CreateUIElement("ButtonContainer", pausePanel.transform);
            SetRect(btnContainer.GetComponent<RectTransform>(), new Vector2(0.5f, 0.45f), new Vector2(400, 360), Vector2.zero);

            VerticalLayoutGroup vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            Button resumeBtn = CreateStyledButton("ResumeButton", btnContainer.transform, "RESUME", buttonSprite, font, 24);
            Button restartBtn = CreateStyledButton("RestartButton", btnContainer.transform, "RESTART", buttonSprite, font, 24);
            Button mainBtn = CreateStyledButton("MainMenuButton", btnContainer.transform, "MAIN MENU", buttonSprite, font, 24);

            UnityEventTools.AddPersistentListener(resumeBtn.onClick, pauseMgr.Resume);
            UnityEventTools.AddPersistentListener(restartBtn.onClick, pauseMgr.RestartLevel);
            UnityEventTools.AddPersistentListener(mainBtn.onClick, pauseMgr.LoadMainMenu);

            SerializedObject so = new SerializedObject(pauseMgr);
            so.FindProperty("pauseMenuUI").objectReferenceValue = pausePanel;
            so.FindProperty("pauseButtonObj").objectReferenceValue = pauseBtnObj;
            so.ApplyModifiedProperties();

            pausePanel.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootObj, resPrefabPath);
            Object.DestroyImmediate(rootObj);

            Debug.Log("<color=green>[Musashi] PauseMenuPrefab created at: " + prefabPath + " and " + resPrefabPath + "</color>");
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static Button CreateStyledButton(string name, Transform parent, string text, Sprite bgSprite, TMP_FontAsset font, int fontSize, float width = 380, float height = 60)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            Image img = btnObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
            }
            else
            {
                img.color = new Color(0.25f, 0.2f, 0.15f, 1f);
            }

            Button btn = btnObj.AddComponent<Button>();

            GameObject textObj = CreateUIElement("Text", btnObj.transform);
            SetFullStretch(textObj.GetComponent<RectTransform>());
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (font != null) tmp.font = font;

            return btn;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
