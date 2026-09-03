using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using Opsive.UltimateInventorySystem.Demo.CharacterControl;
using Opsive.UltimateInventorySystem.Demo.CharacterControl.Player;
using Game.Runtime.Character;
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
            EnsureFolder("Assets/_Game/Prefabs/Player");
            EnsureFolder("Assets/_Game/Animations/Player");

            // 动画控制器只在首次创建时复制（避免 GUID 抖动破坏引用）
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(TargetAnimator) == null) {
                AssetDatabase.CopyAsset(SourceAnimator, TargetAnimator);
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(TargetAnimator);
            AddSparkParameters(controller);

            // 始终从 Demo 源预制体重建（覆盖同一路径、保留 meta/GUID）。
            // 这样能自愈历史构建残留——例如曾误删 Opsive 输入子物体导致的坏预制体。
            var root = PrefabUtility.LoadPrefabContents(SourcePrefab);
            root.tag = "Player";

            var playerCharacter = root.GetComponent<PlayerCharacter>();
            if (playerCharacter != null) {
                var pcSo = new SerializedObject(playerCharacter);
                pcSo.FindProperty("m_EnableMovement").boolValue = false;
                pcSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var characterCamera = root.GetComponent<CharacterCamera>();
            if (characterCamera != null) { Object.DestroyImmediate(characterCamera); }

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
            // 地面检测排除玩家自身图层（Demo 玩家根在 Opsive 图层 8），其余图层全部视为地面。
            // Demo 场景的地面在 Default 图层，此前只查 "Ground" 图层导致 isGrounded 恒为 false。
            int groundMask = ~0 & ~(1 << root.layer);
            tpcSo.FindProperty("groundLayerMask").intValue = groundMask;
            tpcSo.ApplyModifiedPropertiesWithoutUndo();

            var playerInput = root.GetComponent<PlayerInput>();
            if (playerInput == null) { playerInput = root.AddComponent<PlayerInput>(); }
            playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ThirdPersonInput);
            playerInput.defaultActionMap = "Player";
            // 关键：指定控制方案，否则 PlayerInput.devices 为空 → 动作没有任何 controls → 键盘输入完全不生效。
            playerInput.defaultControlScheme = "KeyboardMouse";
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

            // 外观应用器：出生时读取角色创建数据（换色/换装）
            root.AddComponent<PlayerCustomizationApplier>();

            // 直接订阅输入动作驱动控制器，绕开 PlayerInput.SendMessages（与 Opsive 输入组件的冲突/时序问题）
            var feeder = root.AddComponent<SparkInputFeeder>();
            var feederSo = new SerializedObject(feeder);
            feederSo.FindProperty("inputActions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(ThirdPersonInput);
            feederSo.ApplyModifiedPropertiesWithoutUndo();

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
