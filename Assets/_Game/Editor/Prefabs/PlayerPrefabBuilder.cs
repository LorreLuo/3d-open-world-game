using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using Opsive.UltimateInventorySystem.Demo.CharacterControl;
using Opsive.UltimateInventorySystem.Demo.CharacterControl.Player;
using Opsive.UltimateInventorySystem.Demo.Damageable;
using Game.Runtime.Player;

namespace Game.Editor
{
    public static class PlayerPrefabBuilder
    {
        const string SourcePrefab = "Assets/Samples/Opsive Ultimate Inventory System/1.3.8/Demo/Prefabs/Characters/Player/Player Character.prefab";
        const string TargetPrefab = "Assets/_Game/Prefabs/Player/PlayerCharacter.prefab";
        const string SourceAnimator = "Assets/Samples/Opsive Ultimate Inventory System/1.3.8/Demo/Animations/Character/Character.controller";
        const string TargetAnimator = "Assets/_Game/Animations/Player/PlayerAnimator.controller";
        const string ThirdPersonInput = "Assets/Blink/Spark/Core/Runtime/CharacterControllers/ThirdPerson/ThirdPersonInput.inputactions";
        const string InteractInput = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Game/Build/Player Prefab")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs/Player")) {
                DatabaseGenerator_EnsureFolder("Assets/_Game/Prefabs/Player");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Animations/Player")) {
                DatabaseGenerator_EnsureFolder("Assets/_Game/Animations/Player");
            }

            AssetDatabase.CopyAsset(SourcePrefab, TargetPrefab);
            AssetDatabase.CopyAsset(SourceAnimator, TargetAnimator);
            AssetDatabase.ImportAsset(TargetPrefab);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(TargetAnimator);
            AddSparkParameters(controller);

            // 读取旧 prefab 中待迁移的序列化字段
            var sourceRoot = PrefabUtility.LoadPrefabContents(SourcePrefab);
            var sourceCharacterSo = new SerializedObject(sourceRoot.GetComponent<PlayerCharacter>());
            var srcStatsProp = sourceCharacterSo.FindProperty("m_BaseStats");
            var respawnOnDeath = sourceCharacterSo.FindProperty("m_RespawnOnDeath").boolValue;
            var itemHotbar = sourceCharacterSo.FindProperty("m_ItemHotbar").objectReferenceValue;
            var sourceDamageableSo = new SerializedObject(sourceRoot.GetComponent<DemoCharacterDamageable>());
            var srcFlashProp = sourceDamageableSo.FindProperty("m_Flash");
            PrefabUtility.UnloadPrefabContents(sourceRoot);

            // 编辑新 prefab
            var root = PrefabUtility.LoadPrefabContents(TargetPrefab);
            root.tag = "Player";

            foreach (var comp in new List<Component> {
                root.GetComponent<PlayerCharacter>(),
                root.GetComponent<DemoCharacterDamageable>(),
                root.GetComponent<CharacterCamera>(),
            }) {
                if (comp != null) { Object.DestroyImmediate(comp); }
            }

            var animator = root.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            var sparkEntity = root.AddComponent<SparkEntity>();
            var sparkEntitySo = new SerializedObject(sparkEntity);
            if (sparkEntitySo.FindProperty("isLocalPlayer") != null) {
                sparkEntitySo.FindProperty("isLocalPlayer").boolValue = true;
                sparkEntitySo.ApplyModifiedPropertiesWithoutUndo();
            }

            root.AddComponent<RulesEntity>();

            var tpc = root.AddComponent<SparkThirdPersonController>();
            var tpcSo = new SerializedObject(tpc);
            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) {
                tpcSo.FindProperty("groundLayerMask").intValue = 1 << groundLayer;
            }
            tpcSo.ApplyModifiedPropertiesWithoutUndo();

            var playerInput = root.GetComponent<PlayerInput>();
            if (playerInput == null) { playerInput = root.AddComponent<PlayerInput>(); }
            playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ThirdPersonInput);
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;

            var interactor = root.AddComponent<InteractorEntity>();
            var interactorSo = new SerializedObject(interactor);
            interactorSo.FindProperty("inputActionAsset").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InteractInput);
            interactorSo.ApplyModifiedPropertiesWithoutUndo();

            var followTargetGo = new GameObject("CameraFollowTarget");
            followTargetGo.transform.SetParent(root.transform, false);
            followTargetGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            tpcSo.FindProperty("cameraFollowTarget").objectReferenceValue = followTargetGo;
            tpcSo.ApplyModifiedPropertiesWithoutUndo();

            var gpc = root.AddComponent<GamePlayerCharacter>();
            var gpcSo = new SerializedObject(gpc);
            CopySerializedValue(srcStatsProp, gpcSo.FindProperty("m_BaseStats"));
            gpcSo.FindProperty("m_RespawnOnDeath").boolValue = respawnOnDeath;
            gpcSo.FindProperty("m_ItemHotbar").objectReferenceValue = itemHotbar;
            gpcSo.ApplyModifiedPropertiesWithoutUndo();

            var gpd = root.AddComponent<GamePlayerDamageable>();
            var gpdSo = new SerializedObject(gpd);
            gpdSo.FindProperty("m_Character").objectReferenceValue = gpc;
            CopySerializedValue(srcFlashProp, gpdSo.FindProperty("m_Flash"));
            gpdSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, TargetPrefab);
            PrefabUtility.UnloadPrefabContents(root);

            AssetDatabase.SaveAssets();
            Debug.Log("PLAYER_PREFAB_DONE");
        }

        static void AddSparkParameters(AnimatorController controller)
        {
            var has = new HashSet<string>();
            foreach (var p in controller.parameters) { has.Add(p.name); }

            void Add(string name, AnimatorControllerParameterType type)
            {
                if (!has.Contains(name)) { controller.AddParameter(name, type); has.Add(name); }
            }

            Add("Speed", AnimatorControllerParameterType.Float);
            Add("State", AnimatorControllerParameterType.Float);
            Add("MotionSpeed", AnimatorControllerParameterType.Float);
            Add("StrafeX", AnimatorControllerParameterType.Float);
            Add("StrafeY", AnimatorControllerParameterType.Float);
            Add("StateInt", AnimatorControllerParameterType.Int);
            Add("Grounded", AnimatorControllerParameterType.Bool);
            Add("Jump", AnimatorControllerParameterType.Bool);
            Add("FreeFall", AnimatorControllerParameterType.Bool);
            Add("TargetLocked", AnimatorControllerParameterType.Bool);
            string[] triggers = {
                "Roll Forward", "Roll Forward Left", "Roll Forward Right", "Roll Left",
                "Roll Right", "Roll Backward Left", "Roll Backward Right", "Roll Backward"
            };
            foreach (var t in triggers) { Add(t, AnimatorControllerParameterType.Trigger); }
        }

        /// <summary>
        /// 把普通 [Serializable] 类字段的序列化值递归复制（objectReferenceValue 对非 Object 无效）。
        /// </summary>
        static void CopySerializedValue(SerializedProperty src, SerializedProperty dst)
        {
            if (src == null || dst == null) { return; }
            switch (dst.propertyType) {
                case SerializedPropertyType.Generic:
                    var end = src.GetEndProperty();
                    var it = src.Copy();
                    if (it.Next(true)) {
                        do {
                            if (SerializedProperty.EqualContents(it, end)) { break; }
                            var relPath = it.propertyPath.Substring(src.propertyPath.Length + 1);
                            var rel = dst.FindPropertyRelative(relPath);
                            if (rel != null) { CopySerializedValue(it, rel); }
                        } while (it.Next(false));
                    }
                    break;
                case SerializedPropertyType.ObjectReference: dst.objectReferenceValue = src.objectReferenceValue; break;
                case SerializedPropertyType.Integer: dst.intValue = src.intValue; break;
                case SerializedPropertyType.Float: dst.floatValue = src.floatValue; break;
                case SerializedPropertyType.Boolean: dst.boolValue = src.boolValue; break;
                case SerializedPropertyType.String: dst.stringValue = src.stringValue; break;
                case SerializedPropertyType.Color: dst.colorValue = src.colorValue; break;
                case SerializedPropertyType.Vector2: dst.vector2Value = src.vector2Value; break;
                case SerializedPropertyType.Vector3: dst.vector3Value = src.vector3Value; break;
                case SerializedPropertyType.Enum: dst.enumValueIndex = src.enumValueIndex; break;
            }
        }

        static void DatabaseGenerator_EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) { DatabaseGenerator_EnsureFolder(parent); }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
