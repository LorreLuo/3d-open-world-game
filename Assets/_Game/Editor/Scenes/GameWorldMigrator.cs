using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using Opsive.UltimateInventorySystem.Demo.Interaction.Interactables;
using Opsive.UltimateInventorySystem.Interactions;
using Opsive.UltimateInventorySystem.UI.Menus;
using Opsive.UltimateInventorySystem.UI.Menus.Chest;
using Opsive.UltimateInventorySystem.UI.Panels;
using Game.Bridge;
using Game.Runtime;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class GameWorldMigrator
    {
        const string SourceScene = "Assets/Samples/Opsive Ultimate Inventory System/1.3.8/Demo/Demo.unity";
        const string TargetScene = "Assets/_Game/Scenes/GameWorld.unity";
        const string PlayerPrefab = "Assets/_Game/Prefabs/Player/PlayerCharacter.prefab";
        const string CameraRigPrefab = "Assets/_Game/Prefabs/Camera/PlayerCameraRig.prefab";
        const string LoadingScreenPrefab = "Assets/_Game/Prefabs/UI/LoadingScreen.prefab";
        const string InteractionCanvasPrefab = "Assets/_Game/Prefabs/UI/InteractionCanvas.prefab";
        const string GameWorldEntry = "Assets/_Game/Data/Resources/Database/Scenes/GameWorld.asset";

        [MenuItem("Game/Build/Migrate GameWorld Scene")]
        public static void Migrate()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes")) {
                AssetDatabase.CreateFolder("Assets/_Game", "Scenes");
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) == null) {
                AssetDatabase.CopyAsset(SourceScene, TargetScene);
            }
            var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);

            // 1. 记录旧玩家位置
            var oldPlayer = GameObject.Find("Player Character");
            Vector3 playerPos = Vector3.zero;
            Quaternion playerRot = Quaternion.identity;
            Vector3 playerScale = Vector3.one;
            Transform playerParent = null;
            if (oldPlayer != null) {
                playerPos = oldPlayer.transform.position;
                playerRot = oldPlayer.transform.rotation;
                playerScale = oldPlayer.transform.localScale;
                playerParent = oldPlayer.transform.parent;
            }

            // 2. 替换玩家
            GameObject newPlayer = null;
            if (oldPlayer != null) {
                newPlayer = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab), playerParent);
                newPlayer.transform.SetPositionAndRotation(playerPos, playerRot);
                newPlayer.transform.localScale = playerScale;
                newPlayer.name = "Player";
                Object.DestroyImmediate(oldPlayer);
            }

            // 3. 相机替换
            var oldCamera = GameObject.Find("Main Camera");
            if (oldCamera != null) { Object.DestroyImmediate(oldCamera); }
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefab));

            // 4. LoadingScreen + InteractionCanvas
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LoadingScreenPrefab));
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InteractionCanvasPrefab));

            // 5. 交互物改造
            RewireInventoryMenu<ShopMenuOpener>("Shop");
            RewireInventoryMenu<CraftingMenuOpener>("CraftStable");
            RewireInventoryMenu<StorageMenuOpener>("StorageHouse");
            RewirePanelOpener<PanelOpener>("UpgradeStable");
            RewireChests();
            RewireGates();

            // 6. 回填出生点
            var entry = AssetDatabase.LoadAssetAtPath<SceneEntry>(GameWorldEntry);
            if (entry != null) {
                entry.defaultSpawnPosition = playerPos;
                entry.defaultSpawnRotation = playerRot.eulerAngles;
                EditorUtility.SetDirty(entry);
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("GAMEWORLD_MIGRATE_DONE");
        }

        /// <summary>
        /// 给 GameWorld 场景添加运行时引导组件：恢复 timeScale 并关闭 Demo 欢迎面板（它自动打开会把时间暂停导致无法移动）。
        /// </summary>
        [MenuItem("Game/Build/Add GameWorld Bootstrap")]
        public static void AddGameWorldBootstrap()
        {
            var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
            var existing = Object.FindObjectsByType<GameWorldBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Length == 0) {
                var go = new GameObject("GameWorldBootstrap");
                go.AddComponent<GameWorldBootstrap>();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EditorSceneManager.SaveScene(scene);
            Debug.Log("GAMEWORLD_BOOTSTRAP_ADDED");
        }

        static void RewireInventoryMenu<T>(string displayName) where T : InventoryPanelOpener
        {
            foreach (var opener in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var bridge = Rewire(opener.gameObject);
                var so = new SerializedObject(bridge);
                so.FindProperty("m_InventoryPanelOpener").objectReferenceValue = opener;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void RewirePanelOpener<T>(string displayName) where T : PanelOpener
        {
            foreach (var opener in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var bridge = Rewire(opener.gameObject);
                var so = new SerializedObject(bridge);
                so.FindProperty("m_PanelOpener").objectReferenceValue = opener;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void RewireChests()
        {
            foreach (var chest in Object.FindObjectsByType<Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var bridge = Rewire(chest.gameObject);
                var so = new SerializedObject(bridge);
                so.FindProperty("m_Chest").objectReferenceValue = chest;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void RewireGates()
        {
            foreach (var gate in Object.FindObjectsByType<GateDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var go = gate.gameObject;
                var gateSo = new SerializedObject(gate);
                var key = gateSo.FindProperty("m_GateKey").objectReferenceValue;
                var anim = gateSo.FindProperty("m_Animator").objectReferenceValue;
                var panel = gateSo.FindProperty("m_TextPanel").objectReferenceValue;
                var noKey = gateSo.FindProperty("m_TextIfNoKey").stringValue;
                var hasKey = gateSo.FindProperty("m_TextHasKey").stringValue;
                var time = gateSo.FindProperty("m_TextDisplayTime").floatValue;
                Object.DestroyImmediate(gate);

                Rewire(go);
                var bridge = go.AddComponent<GateDoorBridge>();
                var so = new SerializedObject(bridge);
                so.FindProperty("m_GateKey").objectReferenceValue = key;
                so.FindProperty("m_Animator").objectReferenceValue = anim;
                so.FindProperty("m_TextPanel").objectReferenceValue = panel;
                so.FindProperty("m_TextIfNoKey").stringValue = string.IsNullOrEmpty(noKey) ? "需要大门钥匙。" : noKey;
                so.FindProperty("m_TextHasKey").stringValue = string.IsNullOrEmpty(hasKey) ? "大门已打开。" : hasKey;
                so.FindProperty("m_TextDisplayTime").floatValue = time;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static UisMenuBridge Rewire(GameObject target)
        {
            var interactableChild = target.transform.Find("Interactable");
            if (interactableChild != null) { Object.DestroyImmediate(interactableChild.gameObject); }
            var interactableComp = target.GetComponent<Interactable>();
            if (interactableComp != null) { Object.DestroyImmediate(interactableComp); }

            var ioe = target.AddComponent<InteractableObjectEntity>();
            var ioeSo = new SerializedObject(ioe);
            ioeSo.FindProperty("displayName").stringValue = target.name;
            ioeSo.FindProperty("allowMultipleInteractions").boolValue = true;
            ioeSo.ApplyModifiedPropertiesWithoutUndo();

            var bridge = target.AddComponent<UisMenuBridge>();
            UnityEventTools.AddPersistentListener(ioe.OnInteract, bridge.Open);
            return bridge;
        }
    }
}
