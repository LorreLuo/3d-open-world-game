using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Game.Runtime.MainMenu;

namespace Game.Editor
{
    public static class MainMenuSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        const string LoadingScreenPrefab = "Assets/_Game/Prefabs/UI/LoadingScreen.prefab";
        const string GameWorldEntry = "Assets/_Game/Data/Resources/Database/Scenes/GameWorld.asset";
        const string KeybindRowPrefab = "Assets/Blink/Spark/Core/Plugins/GameSettings/Runtime/UI Prefabs/KeybindRow.prefab";
        const string KeybindCategoryTitlePrefab = "Assets/Blink/Spark/Core/Plugins/GameSettings/Runtime/UI Prefabs/KeybindCategoryTitle.prefab";

        [MenuItem("Game/Build/MainMenu Scene")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes")) {
                AssetDatabase.CreateFolder("Assets/_Game", "Scenes");
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相机
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
            camGo.AddComponent<AudioListener>();

            // EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var font = UiPrefabBuilder.EnsureCjkFont();

            // Canvas
            var canvasGo = new GameObject("MainMenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 主菜单面板
            var panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            UiPrefabBuilder.CreateText(panel.transform, "Title", "开放世界生存", 84,
                UiPrefabBuilder.Normalized(0.5f, 0.80f), font, TextAlignmentOptions.Center);
            UiPrefabBuilder.CreateText(panel.transform, "Subtitle", "Spark × Ultimate Inventory System 演示", 32,
                UiPrefabBuilder.Normalized(0.5f, 0.73f), font, TextAlignmentOptions.Center);

            var newGameBtn = CreateButton(panel.transform, "NewGameButton", "新游戏", 0.55f, font);
            var continueBtn = CreateButton(panel.transform, "ContinueButton", "继续游戏", 0.46f, font);
            var settingsBtn = CreateButton(panel.transform, "SettingsButton", "设置", 0.37f, font);
            var quitBtn = CreateButton(panel.transform, "QuitButton", "退出游戏", 0.28f, font);

            // 设置面板
            BuildGameSettingsPanel(canvasGo.transform, font);

            // LoadingScreen 实例
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LoadingScreenPrefab));

            // 流程控制
            var flowGo = new GameObject("MainMenuFlow");
            var flow = flowGo.AddComponent<MainMenuFlow>();
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("m_GameWorldSceneEntry").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SceneEntry>(GameWorldEntry);
            flowSo.FindProperty("m_ContinueButton").objectReferenceValue = continueBtn.GetComponent<Button>();
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(newGameBtn.GetComponent<Button>().onClick, flow.OnNewGame);
            UnityEventTools.AddPersistentListener(continueBtn.GetComponent<Button>().onClick, flow.OnContinue);
            UnityEventTools.AddPersistentListener(settingsBtn.GetComponent<Button>().onClick, flow.OnSettings);
            UnityEventTools.AddPersistentListener(quitBtn.GetComponent<Button>().onClick, flow.OnQuit);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("MAINMENU_SCENE_DONE");
        }

        static GameObject CreateButton(Transform parent, string name, string label, float y01, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 72);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, (y01 - 0.5f) * 1080f);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.18f, 0.24f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            return go;
        }

        static void BuildGameSettingsPanel(Transform canvasTransform, TMP_FontAsset font)
        {
            var settingsRoot = new GameObject("GameSettings");
            settingsRoot.transform.SetParent(canvasTransform, false);
            settingsRoot.AddComponent<GameSettingsManager>();

            var panelGo = new GameObject("GameSettingsPanel");
            // 直接挂到 Canvas 下，保证全屏 RectTransform 锚定生效（settingsRoot 是普通 Transform，会破坏子级 RectTransform 布局）
            panelGo.transform.SetParent(canvasTransform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelCg = panelGo.AddComponent<CanvasGroup>();
            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.08f, 0.96f);

            var gsu = panelGo.AddComponent<GameSettingsUI>();
            var gsuSo = new SerializedObject(gsu);
            gsuSo.FindProperty("panelCanvasGroup").objectReferenceValue = panelCg;

            // 页签行
            var tabsRow = new GameObject("CategoryTabs");
            tabsRow.transform.SetParent(panelGo.transform, false);
            var tabsRowRect = tabsRow.AddComponent<RectTransform>();
            tabsRowRect.anchorMin = new Vector2(0.5f, 1);
            tabsRowRect.anchorMax = new Vector2(0.5f, 1);
            tabsRowRect.pivot = new Vector2(0.5f, 1);
            tabsRowRect.sizeDelta = new Vector2(900, 64);
            tabsRowRect.anchoredPosition = new Vector2(0, -40);

            var (videoTab, videoIndicator) = CreateTab(tabsRow.transform, "VideoTab", "视频", 0, font);
            var (audioTab, audioIndicator) = CreateTab(tabsRow.transform, "AudioTab", "音频", 1, font);
            var (keybindTab, keybindIndicator) = CreateTab(tabsRow.transform, "KeybindTab", "键位", 2, font);

            // 三个视图
            var videoView = CreateView(panelGo.transform, "VideoView", font, BuildVideoView);
            var audioView = CreateView(panelGo.transform, "AudioView", font, BuildAudioView);
            var keybindView = CreateView(panelGo.transform, "KeybindView", font, BuildKeybindView);

            var tabsProp = gsuSo.FindProperty("categoryTabs");
            tabsProp.arraySize = 3;
            SetTab(tabsProp.GetArrayElementAtIndex(0), videoTab, videoIndicator, videoView);
            SetTab(tabsProp.GetArrayElementAtIndex(1), audioTab, audioIndicator, audioView);
            SetTab(tabsProp.GetArrayElementAtIndex(2), keybindTab, keybindIndicator, keybindView);
            gsuSo.FindProperty("defaultCategoryIndex").intValue = 0;
            gsuSo.FindProperty("hideOnStart").boolValue = true;
            gsuSo.ApplyModifiedPropertiesWithoutUndo();

            // 初始隐藏：hideOnStart 的 ClosePanel 对从未打开的面板不生效（isPanelOpen 为 false 时直接 return），
            // 因此必须把 CanvasGroup 序列化为 alpha=0 的隐藏态，OpenPanel 时才会置 1。
            panelCg.alpha = 0f;
            panelCg.interactable = false;
            panelCg.blocksRaycasts = false;

            // 关闭按钮
            var closeBtn = CreateButton(panelGo.transform, "CloseSettingsButton", "关闭", 0f, font);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(160, 56);
            closeRect.anchoredPosition = new Vector2(-40, -40);
            UnityEventTools.AddPersistentListener(closeBtn.GetComponent<Button>().onClick, gsu.HideSettings);

            EditorUtility.SetDirty(panelGo);
        }

        static (Button, GameObject) CreateTab(Transform parent, string name, string label, int index, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 64);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2((index - 1) * 270f, 0);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.14f, 0.16f, 0.22f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 30;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            var indicator = new GameObject("SelectedIndicator");
            indicator.transform.SetParent(go.transform, false);
            var indicatorRt = indicator.AddComponent<RectTransform>();
            indicatorRt.anchorMin = new Vector2(0, 0);
            indicatorRt.anchorMax = new Vector2(1, 0);
            indicatorRt.pivot = new Vector2(0.5f, 0);
            indicatorRt.sizeDelta = new Vector2(0, 6);
            var indicatorImg = indicator.AddComponent<Image>();
            indicatorImg.color = new Color(0.35f, 0.65f, 0.95f);
            indicator.SetActive(false);
            return (btn, indicator);
        }

        static CanvasGroup CreateView(Transform parent, string name, TMP_FontAsset font, System.Action<Transform, TMP_FontAsset> populate)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -120);
            var cg = go.AddComponent<CanvasGroup>();
            populate(go.transform, font);
            return cg;
        }

        static void BuildVideoView(Transform view, TMP_FontAsset font)
        {
            var ui = view.gameObject.AddComponent<VideoSettingsUI>();
            var so = new SerializedObject(ui);
            var items = new (string label, string prop)[]
            {
                ("分辨率", "resolutionDropdown"), ("窗口模式", "windowModeDropdown"),
                ("帧率上限", "frameRateLimitDropdown"), ("画质", "qualityDropdown"),
                ("抗锯齿", "antiAliasingDropdown"), ("纹理质量", "textureQualityDropdown"),
            };
            for (int i = 0; i < items.Length; i++) {
                UiPrefabBuilder.CreateText(view, items[i].label + "Label", items[i].label, 28,
                    new Vector2(-420, 260 - i * 110), font, TextAlignmentOptions.Left);
                var dropdown = CreateTMPDropdown(view, items[i].label + "Dropdown", font, new Vector2(180, 260 - i * 110));
                so.FindProperty(items[i].prop).objectReferenceValue = dropdown;
            }
            var vsyncToggle = CreateToggle(view, "VsyncToggle", "垂直同步", font, new Vector2(180, 260 - 6 * 110));
            so.FindProperty("vSyncToggle").objectReferenceValue = vsyncToggle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildAudioView(Transform view, TMP_FontAsset font)
        {
            var ui = view.gameObject.AddComponent<AudioSettingsUI>();
            var so = new SerializedObject(ui);
            var items = new (string label, string prop)[]
            {
                ("主音量", "masterVolumeSlider"), ("音乐音量", "musicVolumeSlider"),
                ("音效音量", "sfxVolumeSlider"), ("界面音量", "uiVolumeSlider"),
            };
            for (int i = 0; i < items.Length; i++) {
                UiPrefabBuilder.CreateText(view, items[i].label + "Label", items[i].label, 28,
                    new Vector2(-420, 200 - i * 140), font, TextAlignmentOptions.Left);
                var slider = CreateSlider(view, items[i].label + "Slider", new Vector2(180, 200 - i * 140));
                so.FindProperty(items[i].prop).objectReferenceValue = slider;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildKeybindView(Transform view, TMP_FontAsset font)
        {
            var ui = view.gameObject.AddComponent<KeybindSettingsUI>();
            var so = new SerializedObject(ui);

            var container = new GameObject("KeybindContainer");
            container.transform.SetParent(view, false);
            var containerRt = container.AddComponent<RectTransform>();
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.offsetMin = new Vector2(60, 60);
            containerRt.offsetMax = new Vector2(-60, -160);
            so.FindProperty("keybindContainer").objectReferenceValue = container.transform;
            so.FindProperty("keybindRowPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(KeybindRowPrefab);
            so.FindProperty("keybindCategoryTitlePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(KeybindCategoryTitlePrefab);

            // 改绑遮罩
            var overlay = new GameObject("RebindOverlay");
            overlay.transform.SetParent(view, false);
            var overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.8f);
            var overlayCg = overlay.AddComponent<CanvasGroup>();
            var title = UiPrefabBuilder.CreateText(overlay.transform, "Title", "按下新按键", 44,
                UiPrefabBuilder.Normalized(0.5f, 0.62f), font, TextAlignmentOptions.Center);
            var pressKey = UiPrefabBuilder.CreateText(overlay.transform, "PressKeyText", "等待输入…", 36,
                UiPrefabBuilder.Normalized(0.5f, 0.52f), font, TextAlignmentOptions.Center);
            var warningGo = new GameObject("BindAlreadyInUseWarning");
            warningGo.transform.SetParent(overlay.transform, false);
            var warningRt = warningGo.AddComponent<RectTransform>();
            warningRt.anchorMin = warningRt.anchorMax = new Vector2(0.5f, 0.5f);
            warningRt.sizeDelta = new Vector2(900, 60);
            warningRt.anchoredPosition = new Vector2(0, -120);
            var warningText = warningGo.AddComponent<TextMeshProUGUI>();
            warningText.font = font;
            warningText.fontSize = 28;
            warningText.alignment = TextAlignmentOptions.Center;
            warningText.text = "该按键已被占用";
            warningText.color = new Color(0.9f, 0.3f, 0.3f);
            var resetBtn = CreateButton(overlay.transform, "ResetToDefaultButton", "恢复默认", 0.30f, font);
            var confirmBtn = CreateButton(overlay.transform, "ConfirmButton", "确认", 0.20f, font);
            var closeBtn = CreateButton(overlay.transform, "CloseButton", "取消", 0.10f, font);
            overlay.SetActive(false);

            var rebindUi = overlay.AddComponent<KeybindRebindOverlayUI>();
            so.FindProperty("rebindOverlay").objectReferenceValue = rebindUi;
            var rebindSo = new SerializedObject(rebindUi);
            rebindSo.FindProperty("overlayCanvasGroup").objectReferenceValue = overlayCg;
            rebindSo.FindProperty("titleText").objectReferenceValue = title;
            rebindSo.FindProperty("pressKeyText").objectReferenceValue = pressKey;
            rebindSo.FindProperty("bindAlreadyInUsedWarningText").objectReferenceValue = warningText;
            rebindSo.FindProperty("resetToDefaultButton").objectReferenceValue = resetBtn.GetComponent<Button>();
            rebindSo.FindProperty("confirmButton").objectReferenceValue = confirmBtn.GetComponent<Button>();
            rebindSo.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            rebindSo.FindProperty("bindAlreadyInUsedWarning").objectReferenceValue = warningGo;
            rebindSo.ApplyModifiedPropertiesWithoutUndo();

            // 重置全部按钮
            var resetAll = CreateButton(view, "ResetAllButton", "恢复全部默认", 0.08f, font);
            resetAll.GetComponent<RectTransform>().anchorMin = resetAll.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            resetAll.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -300);
            so.FindProperty("resetAllButton").objectReferenceValue = resetAll.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_Dropdown CreateTMPDropdown(Transform parent, string name, TMP_FontAsset font, Vector2 anchoredPos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340, 44);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            var image = root.AddComponent<Image>();
            image.color = new Color(0.14f, 0.16f, 0.22f);
            var dropdown = root.AddComponent<TMP_Dropdown>();

            var captionGo = new GameObject("Label");
            captionGo.transform.SetParent(root.transform, false);
            var captionRt = captionGo.AddComponent<RectTransform>();
            captionRt.anchorMin = Vector2.zero;
            captionRt.anchorMax = Vector2.one;
            captionRt.offsetMin = new Vector2(10, 6);
            captionRt.offsetMax = new Vector2(-30, -6);
            var captionText = captionGo.AddComponent<TextMeshProUGUI>();
            captionText.font = font;
            captionText.fontSize = 24;
            captionText.alignment = TextAlignmentOptions.Left;

            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(root.transform, false);
            var arrowRt = arrowGo.AddComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0.5f);
            arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.sizeDelta = new Vector2(20, 20);
            arrowRt.anchoredPosition = new Vector2(-15, 0);
            var arrowText = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.font = font;
            arrowText.fontSize = 16;
            arrowText.alignment = TextAlignmentOptions.Center;

            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(root.transform, false);
            var templateRt = templateGo.AddComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1);
            templateRt.sizeDelta = new Vector2(0, 220);
            templateRt.anchoredPosition = Vector2.zero;
            templateGo.AddComponent<Image>();
            var scrollRect = templateGo.AddComponent<ScrollRect>();

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(templateGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGo.AddComponent<Image>();
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 0);

            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRt = itemGo.AddComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 1);
            itemRt.anchorMax = new Vector2(1, 1);
            itemRt.sizeDelta = new Vector2(0, 44);
            itemGo.AddComponent<Toggle>();
            itemGo.AddComponent<Image>();
            var itemLabelGo = new GameObject("Item Label");
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var itemLabelRt = itemLabelGo.AddComponent<RectTransform>();
            itemLabelRt.anchorMin = Vector2.zero;
            itemLabelRt.anchorMax = Vector2.one;
            itemLabelRt.offsetMin = new Vector2(10, 2);
            itemLabelRt.offsetMax = new Vector2(-10, -2);
            var itemText = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemText.font = font;
            itemText.fontSize = 22;
            itemText.alignment = TextAlignmentOptions.Left;

            dropdown.template = templateRt;
            dropdown.captionText = captionText;
            dropdown.itemText = itemText;
            dropdown.targetGraphic = image;
            templateGo.SetActive(false);
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            return dropdown;
        }

        static Toggle CreateToggle(Transform parent, string name, string label, TMP_FontAsset font, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340, 44);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            var toggle = go.AddComponent<Toggle>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            return toggle;
        }

        static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 24);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.14f, 0.16f, 0.22f);
            var slider = go.AddComponent<Slider>();

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = new Vector2(4, 0);
            fillAreaRt.offsetMax = new Vector2(-4, 0);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.35f, 0.65f, 0.95f);

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20, 20);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.9f, 0.9f, 0.9f);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            return slider;
        }

        static void SetTab(SerializedProperty tabProp, Button button, GameObject indicator, CanvasGroup view)
        {
            tabProp.FindPropertyRelative("categoryButton").objectReferenceValue = button;
            tabProp.FindPropertyRelative("selectedIndicator").objectReferenceValue = indicator;
            tabProp.FindPropertyRelative("categoryView").objectReferenceValue = view;
        }
    }
}
