using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using Opsive.UltimateInventorySystem.Demo.CharacterControl;
using Opsive.UltimateInventorySystem.Demo.CharacterControl.Player;

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

            // 只在首次创建时复制（CopyAsset 会重写 meta GUID，重复复制会破坏引用）
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefab) == null) {
                AssetDatabase.CopyAsset(SourcePrefab, TargetPrefab);
            }
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(TargetAnimator) == null) {
                AssetDatabase.CopyAsset(SourceAnimator, TargetAnimator);
            }
            AssetDatabase.ImportAsset(TargetPrefab);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(TargetAnimator);
            AddSparkParameters(controller);

            // 编辑目标 prefab：保留 demo 的 Character/PlayerCharacter/DemoCharacterDamageable，
            // 只关闭 demo 自身移动（Spark 控制器接管），再叠加 Spark 组件栈。
            var root = PrefabUtility.LoadPrefabContents(TargetPrefab);
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
