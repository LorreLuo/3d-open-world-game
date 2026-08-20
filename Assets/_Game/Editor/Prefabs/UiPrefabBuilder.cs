using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class UiPrefabBuilder
    {
        const string FontPath = "Assets/_Game/Fonts/SimHei SDF.asset";
        const string CameraRigPath = "Assets/_Game/Prefabs/Camera/PlayerCameraRig.prefab";
        const string LoadingScreenPath = "Assets/_Game/Prefabs/UI/LoadingScreen.prefab";
        const string InteractionCanvasPath = "Assets/_Game/Prefabs/UI/InteractionCanvas.prefab";

        public static TMP_FontAsset EnsureCjkFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) {
                // 校验可用性：无字体源则删除重建（修复前生成的废资产会被自动清理）
                if (existing.sourceFontFile != null) { return existing; }
                AssetDatabase.DeleteAsset(FontPath);
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Fonts")) { AssetDatabase.CreateFolder("Assets/_Game", "Fonts"); }

            string fontFile = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Font")) {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var n = System.IO.Path.GetFileName(p).ToLowerInvariant();
                if (n.Contains("simhei") || n.Contains("msyh") || n.Contains("yahei") ||
                    n.Contains("sourcehansans") || n.Contains("notosanssc") || n.Contains("deng")) { fontFile = p; break; }
            }
            if (fontFile == null) {
                var sys = @"C:\Windows\Fonts\simhei.ttf";
                if (!File.Exists(sys)) { throw new System.Exception("未找到中文字体：项目内无 CJK 字体，且 C:\\Windows\\Fonts\\simhei.ttf 不存在。"); }
                var dst = "Assets/_Game/Fonts/simhei.ttf";
                File.Copy(sys, dst, true);
                AssetDatabase.ImportAsset(dst);
                fontFile = dst;
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontFile);
            if (sourceFont == null) { throw new System.Exception("无法加载字体源: " + fontFile); }
            var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            fontAsset.name = "SimHei SDF";
            AssetDatabase.CreateAsset(fontAsset, FontPath);
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null) {
                fontAsset.atlasTextures[0].name = "SimHei SDF Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }
            var mat = fontAsset.material;
            mat.name = "SimHei SDF Atlas Material";
            AssetDatabase.AddObjectToAsset(mat, fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        [MenuItem("Game/Build/UI Prefabs (Font/Camera/Loading/Interaction)")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/_Game/Prefabs/Camera");
            EnsureFolder("Assets/_Game/Prefabs/UI");
            var font = EnsureCjkFont();
            BuildCameraRig();
            BuildLoadingScreen(font);
            BuildInteractionCanvas(font);
            AssetDatabase.SaveAssets();
            Debug.Log("UI_PREFABS_DONE");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) { EnsureFolder(parent); }
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void BuildCameraRig()
        {
            var root = new GameObject("PlayerCameraRig");
            var camGo = new GameObject("Player Camera");
            camGo.transform.SetParent(root.transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineBrain>();

            var followGo = new GameObject("PlayerFollowCamera");
            followGo.transform.SetParent(camGo.transform, false);
            var cmCam = followGo.AddComponent<CinemachineCamera>();
            cmCam.Priority = 10;
            followGo.AddComponent<CinemachineThirdPersonFollow>();

            SavePrefab(root, CameraRigPath);
        }

        static void BuildLoadingScreen(TMP_FontAsset font)
        {
            var root = new GameObject("LoadingScreen");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var cg = root.AddComponent<CanvasGroup>();

            var bgGo = CreateImage(root.transform, "BackgroundImage", new Color(0.06f, 0.07f, 0.10f, 1f));
            var fadeGo = CreateImage(root.transform, "FadeImage", new Color(0f, 0f, 0f, 0f));
            var sceneName = CreateText(root.transform, "SceneName", "正在进入游戏世界…", 64, Normalized(0.5f, 0.62f), font, TextAlignmentOptions.Center);
            var tips = CreateText(root.transform, "Tips", "提示：按 E 与物体交互，Tab 释放光标。", 28, Normalized(0.5f, 0.30f), font, TextAlignmentOptions.Center);
            var pct = CreateText(root.transform, "Percentage", "0%", 36, Normalized(0.5f, 0.38f), font, TextAlignmentOptions.Center);
            var prompt = CreateText(root.transform, "ClickPrompt", "点击继续…", 32, Normalized(0.5f, 0.22f), font, TextAlignmentOptions.Center);

            var barGo = new GameObject("ProgressBar");
            barGo.transform.SetParent(root.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(800, 24);
            barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0.5f);
            barRt.anchoredPosition = Normalized(0.5f, 0.42f);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0.25f, 0.28f, 0.35f);
            var slider = barGo.AddComponent<Slider>();

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(barGo.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = new Vector2(6, 0);
            fillAreaRt.offsetMax = new Vector2(-6, 0);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.35f, 0.65f, 0.95f);

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.interactable = false;

            var mgr = root.AddComponent<LoadingScreenManager>();
            var so = new SerializedObject(mgr);
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.FindProperty("backgroundImage").objectReferenceValue = bgGo.GetComponent<Image>();
            so.FindProperty("fadeImage").objectReferenceValue = fadeGo.GetComponent<Image>();
            so.FindProperty("sceneNameText").objectReferenceValue = sceneName;
            so.FindProperty("progressBar").objectReferenceValue = slider;
            so.FindProperty("percentageText").objectReferenceValue = pct;
            so.FindProperty("tipsText").objectReferenceValue = tips;
            var tipsProp = so.FindProperty("tips");
            tipsProp.arraySize = 3;
            tipsProp.GetArrayElementAtIndex(0).stringValue = "提示：按 E 与物体交互，Tab 释放光标。";
            tipsProp.GetArrayElementAtIndex(1).stringValue = "提示：靠近商店老板可以购买装备。";
            tipsProp.GetArrayElementAtIndex(2).stringValue = "提示：死亡后会自动在出生点重生。";
            so.FindProperty("mustClickAfterLoading").boolValue = false;
            so.FindProperty("delayAfterLoading").floatValue = 0f;
            so.FindProperty("tipSwitchInterval").floatValue = 3f;
            so.FindProperty("fadeDuration").floatValue = 0.5f;
            so.FindProperty("clickPromptText").objectReferenceValue = prompt;
            so.FindProperty("clickPromptMessage").stringValue = "点击继续…";
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, LoadingScreenPath);
        }

        static void BuildInteractionCanvas(TMP_FontAsset font)
        {
            var root = new GameObject("InteractionCanvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var managerGo = new GameObject("InteractablesManager");
            managerGo.transform.SetParent(root.transform, false);
            var managerRt = managerGo.AddComponent<RectTransform>();
            managerRt.anchorMin = Vector2.zero;
            managerRt.anchorMax = Vector2.one;
            managerRt.offsetMin = Vector2.zero;
            managerRt.offsetMax = Vector2.zero;
            var mgr = managerGo.AddComponent<InteractablesManager>();

            var keyGo = new GameObject("InteractableKey");
            keyGo.transform.SetParent(managerGo.transform, false);
            var keyRt = keyGo.AddComponent<RectTransform>();
            keyRt.sizeDelta = new Vector2(420, 130);
            var keyCg = keyGo.AddComponent<CanvasGroup>();

            var nameBg = new GameObject("NameBackground");
            nameBg.transform.SetParent(keyGo.transform, false);
            var nameBgRt = nameBg.AddComponent<RectTransform>();
            nameBgRt.anchorMin = new Vector2(0.5f, 1);
            nameBgRt.anchorMax = new Vector2(0.5f, 1);
            nameBgRt.pivot = new Vector2(0.5f, 1);
            nameBgRt.sizeDelta = new Vector2(300, 48);
            nameBgRt.anchoredPosition = Vector2.zero;
            var nameBgImg = nameBg.AddComponent<Image>();
            nameBgImg.color = new Color(0f, 0f, 0f, 0.7f);
            var objectNameGo = new GameObject("ObjectName");
            objectNameGo.transform.SetParent(nameBg.transform, false);
            var objectNameRt = objectNameGo.AddComponent<RectTransform>();
            objectNameRt.anchorMin = Vector2.zero;
            objectNameRt.anchorMax = Vector2.one;
            objectNameRt.offsetMin = Vector2.zero;
            objectNameRt.offsetMax = Vector2.zero;
            var objectNameText = objectNameGo.AddComponent<TextMeshProUGUI>();
            objectNameText.font = font;
            objectNameText.fontSize = 26;
            objectNameText.alignment = TextAlignmentOptions.Center;
            objectNameText.text = "交互";

            var keyBg = new GameObject("KeybindBackground");
            keyBg.transform.SetParent(keyGo.transform, false);
            var keyBgRt = keyBg.AddComponent<RectTransform>();
            keyBgRt.anchorMin = new Vector2(0.5f, 0);
            keyBgRt.anchorMax = new Vector2(0.5f, 0);
            keyBgRt.pivot = new Vector2(0.5f, 0);
            keyBgRt.sizeDelta = new Vector2(160, 40);
            keyBgRt.anchoredPosition = new Vector2(0, 8);
            var keyBgImg = keyBg.AddComponent<Image>();
            keyBgImg.color = new Color(0f, 0f, 0f, 0.7f);
            var keyTextGo = new GameObject("KeybindText");
            keyTextGo.transform.SetParent(keyBg.transform, false);
            var keyTextRt = keyTextGo.AddComponent<RectTransform>();
            keyTextRt.anchorMin = Vector2.zero;
            keyTextRt.anchorMax = Vector2.one;
            keyTextRt.offsetMin = Vector2.zero;
            keyTextRt.offsetMax = Vector2.zero;
            var keyText = keyTextGo.AddComponent<TextMeshProUGUI>();
            keyText.font = font;
            keyText.fontSize = 26;
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.text = "E";

            var mgrSo = new SerializedObject(mgr);
            mgrSo.FindProperty("interactableKeyCanvasGroup").objectReferenceValue = keyCg;
            mgrSo.FindProperty("indicatorPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blink/Spark/Core/Plugins/UI/Runtime/UI Prefabs/InteractableIndicator.prefab");
            mgrSo.FindProperty("nameBackground").objectReferenceValue = nameBgRt;
            mgrSo.FindProperty("keybindText").objectReferenceValue = keyText;
            mgrSo.FindProperty("objectNameText").objectReferenceValue = objectNameText;
            mgrSo.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, InteractionCanvasPath);
        }

        static GameObject CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size,
            Vector2 anchoredPos, TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000, 120);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.text = content;
            return text;
        }

        public static Vector2 Normalized(float x, float y)
        {
            return new Vector2((x - 0.5f) * 1920f, (y - 0.5f) * 1080f);
        }

        static void SavePrefab(GameObject root, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            } else {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            Object.DestroyImmediate(root);
        }
    }
}
