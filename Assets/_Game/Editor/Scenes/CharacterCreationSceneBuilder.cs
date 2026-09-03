using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Game.Runtime.Character;

namespace Game.Editor
{
    public static class CharacterCreationSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/CharacterCreation.unity";
        const string LoadingScreenPrefab = "Assets/_Game/Prefabs/UI/LoadingScreen.prefab";
        const string PlayerPrefab = "Assets/_Game/Prefabs/Player/PlayerCharacter.prefab";
        const string PreviewPrefab = "Assets/_Game/Prefabs/Character/CharacterPreview.prefab";
        const string LeatherArmorSource = "Assets/Samples/Opsive Ultimate Inventory System/1.3.8/Demo/Prefabs/Items/Armor/LeatherArmor.prefab";
        const string KnightArmorSource = "Assets/Samples/Opsive Ultimate Inventory System/1.3.8/Demo/Prefabs/Items/Armor/KnightArmor.prefab";
        const string OutfitsFolder = "Assets/_Game/Resources/Outfits";
        const string GameWorldEntry = "Assets/_Game/Data/Resources/Database/Scenes/GameWorld.asset";
        const string MainMenuEntry = "Assets/_Game/Data/Resources/Database/Scenes/MainMenu.asset";

        [MenuItem("Game/Build/CharacterCreation Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder("Assets/_Game/Prefabs/Character");
            EnsureFolder(OutfitsFolder);

            // 1. 护甲预制体复制到 Resources（运行时 Resources.Load 使用）
            CopyIfMissing(LeatherArmorSource, $"{OutfitsFolder}/LeatherArmor.prefab");
            CopyIfMissing(KnightArmorSource, $"{OutfitsFolder}/KnightArmor.prefab");

            // 2. 预览预制体：玩家 prefab 的纯视觉版本
            BuildPreviewPrefab();

            // 3. 场景
            BuildScene();
        }

        static void BuildPreviewPrefab()
        {
            // 仅在不存在时创建，避免 GUID 变动（重建时保持稳定）
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefab) == null) {
                AssetDatabase.CopyAsset(PlayerPrefab, PreviewPrefab);
            }
            AssetDatabase.ImportAsset(PreviewPrefab);

            var root = PrefabUtility.LoadPrefabContents(PreviewPrefab);

            // 移除全部 MonoBehaviour（保留 Animator），移除原生物理组件
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var b in behaviours) {
                if (b is Animator) { continue; }
                Object.DestroyImmediate(b);
            }
            var cc = root.GetComponent<CharacterController>();
            if (cc != null) { Object.DestroyImmediate(cc); }
            var rb = root.GetComponent<Rigidbody>();
            if (rb != null) { Object.DestroyImmediate(rb); }

            // 移除非视觉子物体
            foreach (var childName in new[] { "Canvas", "PlayerInput", "CameraFollowTarget", "Interactor Indicator" }) {
                var child = root.transform.Find(childName);
                if (child != null) { Object.DestroyImmediate(child.gameObject); }
            }

            PrefabUtility.SaveAsPrefabAsset(root, PreviewPrefab);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("CHARACTER_PREVIEW_DONE");
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相机
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.15f);
            camGo.transform.position = new Vector3(0f, 1.4f, 3.4f);
            camGo.transform.rotation = Quaternion.Euler(6f, 180f, 0f);
            camGo.AddComponent<AudioListener>();

            // 灯光
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            // 预览角色：按模型包围盒取景，让角色居中在画面里
            var previewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefab);
            var preview = (GameObject)PrefabUtility.InstantiatePrefab(previewPrefab);
            preview.name = "CharacterPreview";
            preview.transform.position = Vector3.zero;

            var previewRenderers = preview.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(preview.transform.position, Vector3.zero);
            foreach (var r in previewRenderers) { bounds.Encapsulate(r.bounds); }
            var targetCenter = new Vector3(0f, 1.0f, 0f);
            preview.transform.position += targetCenter - bounds.center;

            // 相机取景（在预览定位之后再设）
            camGo.transform.position = targetCenter + new Vector3(0f, 0.05f, 3.0f);
            camGo.transform.LookAt(targetCenter);

            // LoadingScreen（SceneLoader 硬依赖；跨场景存活由 PersistentRoot 保证）
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LoadingScreenPrefab));

            // EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var font = UiPrefabBuilder.EnsureCjkFont();

            // Canvas
            var canvasGo = new GameObject("CreationCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            UiPrefabBuilder.CreateText(canvasGo.transform, "Title", "创建你的角色", 56,
                UiPrefabBuilder.Normalized(0.5f, 0.88f), font, TextAlignmentOptions.Center);

            // 发色
            UiPrefabBuilder.CreateText(canvasGo.transform, "HairLabel", "发色", 30,
                new Vector2(-840, 300), font, TextAlignmentOptions.Left);
            for (int i = 0; i < CharacterCreationFlow.HairPresets.Length; i++) {
                CreateSwatch(canvasGo.transform, $"HairSwatch{i}", CharacterCreationFlow.HairPresets[i],
                    "Hair", i, "", new Vector2(-660 + i * 90, 300));
            }

            // 衣服色
            UiPrefabBuilder.CreateText(canvasGo.transform, "ShirtLabel", "衣服", 30,
                new Vector2(-840, 160), font, TextAlignmentOptions.Left);
            for (int i = 0; i < CharacterCreationFlow.ShirtPresets.Length; i++) {
                CreateSwatch(canvasGo.transform, $"ShirtSwatch{i}", CharacterCreationFlow.ShirtPresets[i],
                    "Shirt", i, "", new Vector2(-660 + i * 90, 160));
            }

            // 鞋子色
            UiPrefabBuilder.CreateText(canvasGo.transform, "BootsLabel", "鞋子", 30,
                new Vector2(-840, 20), font, TextAlignmentOptions.Left);
            for (int i = 0; i < CharacterCreationFlow.BootsPresets.Length; i++) {
                CreateSwatch(canvasGo.transform, $"BootsSwatch{i}", CharacterCreationFlow.BootsPresets[i],
                    "Boots", i, "", new Vector2(-660 + i * 90, 20));
            }

            // 护甲
            UiPrefabBuilder.CreateText(canvasGo.transform, "OutfitLabel", "护甲", 30,
                new Vector2(-840, -120), font, TextAlignmentOptions.Left);
            CreateOutfitButton(canvasGo.transform, "OutfitNone", "无护甲", "None", new Vector2(-660, -120), font);
            CreateOutfitButton(canvasGo.transform, "OutfitLeather", "皮甲", "Leather", new Vector2(-530, -120), font);
            CreateOutfitButton(canvasGo.transform, "OutfitKnight", "骑士甲", "Knight", new Vector2(-400, -120), font);

            // 确认 / 返回
            var confirmBtn = CreateTextButton(canvasGo.transform, "ConfirmButton", "确认进入世界", new Vector2(160, -440), new Vector2(300, 64), font);
            var backBtn = CreateTextButton(canvasGo.transform, "BackButton", "返回", new Vector2(-160, -440), new Vector2(200, 64), font);

            // 流程
            var flowGo = new GameObject("CharacterCreationFlow");
            var flow = flowGo.AddComponent<CharacterCreationFlow>();
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("m_PreviewRoot").objectReferenceValue = preview.transform;
            flowSo.FindProperty("m_GameWorldSceneEntry").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SceneEntry>(GameWorldEntry);
            flowSo.FindProperty("m_MainMenuSceneEntry").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SceneEntry>(MainMenuEntry);
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(confirmBtn.GetComponent<Button>().onClick, flow.OnConfirm);
            UnityEventTools.AddPersistentListener(backBtn.GetComponent<Button>().onClick, flow.OnBack);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("CHARACTER_CREATION_SCENE_DONE");
        }

        static void CreateSwatch(Transform parent, string name, Color color, string partName, int presetIndex,
            string outfitId, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 64);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var option = go.AddComponent<CreationOptionButton>();
            var so = new SerializedObject(option);
            so.FindProperty("m_PartName").stringValue = partName;
            so.FindProperty("m_PresetIndex").intValue = presetIndex;
            so.FindProperty("m_OutfitId").stringValue = outfitId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateOutfitButton(Transform parent, string name, string label, string outfitId, Vector2 pos, TMP_FontAsset font)
        {
            var go = CreateTextButton(parent, name, label, pos, new Vector2(150, 56), font);
            var option = go.AddComponent<CreationOptionButton>();
            var so = new SerializedObject(option);
            so.FindProperty("m_PartName").stringValue = "";
            so.FindProperty("m_PresetIndex").intValue = 0;
            so.FindProperty("m_OutfitId").stringValue = outfitId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject CreateTextButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
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
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            return go;
        }

        static void CopyIfMissing(string source, string dest)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dest) == null && AssetDatabase.LoadAssetAtPath<GameObject>(source) != null) {
                AssetDatabase.CopyAsset(source, dest);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) { EnsureFolder(parent); }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
