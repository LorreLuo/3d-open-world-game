using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    public static class DatabaseGenerator
    {
        const string RulesPath = "Assets/_Game/Data/Resources/Database/Rules";
        const string ScenesPath = "Assets/_Game/Data/Resources/Database/Scenes";
        const string SettingsPath = "Assets/_Game/Data/Resources/Settings";
        const string ThirdPersonInput = "Assets/Blink/Spark/Core/Runtime/CharacterControllers/ThirdPerson/ThirdPersonInput.inputactions";
        const string InteractInput = "Assets/InputSystem_Actions.inputactions";

        static readonly (string key, bool value)[] Rules = new[]
        {
            ("MOVEMENT", true), ("JUMPING", true), ("CAMERA_CONTROLS", true),
            ("GROUNDED", false), ("DEAD", false), ("TARGET_LOCKED", false),
            ("UI_OPENED", false), ("ROOT_MOTION", false), ("IMMUNE", false),
            ("IN_DODGE_ROLL", false), ("MOTION_WARP", false), ("IN_COMBAT", false),
        };

        [MenuItem("Game/Generate/Database Assets")]
        public static void Generate()
        {
            EnsureFolder(RulesPath);
            EnsureFolder(ScenesPath);
            EnsureFolder(SettingsPath);

            foreach (var (key, value) in Rules) {
                var assetPath = $"{RulesPath}/{key}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<RuleEntry>(assetPath);
                if (existing != null) {
                    existing.key = key;
                    existing.defaultValue = value;
                    EditorUtility.SetDirty(existing);
                    continue;
                }
                var entry = ScriptableObject.CreateInstance<RuleEntry>();
                entry.id = $"rule.{key}";
                entry.entryName = key;
                entry.displayName = key;
                entry.key = key;
                entry.defaultValue = value;
                AssetDatabase.CreateAsset(entry, assetPath);
            }

            CreateSceneEntry("MainMenu", "MainMenu", "主菜单");
            CreateSceneEntry("GameWorld", "GameWorld", "游戏世界");

            var categoryPath = $"{SettingsPath}/GeneralKeybindCategory.asset";
            var category = AssetDatabase.LoadAssetAtPath<KeybindCategoryEntry>(categoryPath);
            if (category == null) {
                category = ScriptableObject.CreateInstance<KeybindCategoryEntry>();
                category.id = "keybind.general";
                category.entryName = "General";
                category.displayName = "通用";
                category.sortOrder = 0;
                AssetDatabase.CreateAsset(category, categoryPath);
            } else {
                category.sortOrder = 0;
                EditorUtility.SetDirty(category);
            }

            var settingsPath = $"{SettingsPath}/GameSettingsPluginSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<GameSettingsPluginSettings>(settingsPath);
            if (settings == null) {
                settings = ScriptableObject.CreateInstance<GameSettingsPluginSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            var tpsInput = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ThirdPersonInput);
            var interactInput = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InteractInput);
            settings.inputActionAssets = new List<InputActionAsset> { tpsInput, interactInput };
            settings.keybindActions = new List<KeybindActionConfig>
            {
                Keybind(category, "move", "移动", tpsInput, "Player", "Move", 0, false, ""),
                Keybind(category, "jump", "跳跃", tpsInput, "Player", "Jump", 0, false, ""),
                Keybind(category, "sprint", "冲刺", tpsInput, "Player", "Sprint", 0, false, ""),
                Keybind(category, "roll", "翻滚", tpsInput, "Player", "Roll", 0, false, ""),
                Keybind(category, "interact", "交互", interactInput, "Player", "Interact", 0, false, ""),
                Keybind(category, "cursor", "光标", tpsInput, "Player", "EnableCursor", 0, false, ""),
            };
            EditorUtility.SetDirty(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("DATABASE_GENERATE_DONE");
        }

        static KeybindActionConfig Keybind(KeybindCategoryEntry category, string actionId, string displayName,
            InputActionAsset asset, string map, string action, int bindingIndex, bool composite, string compositePart)
        {
            return new KeybindActionConfig {
                category = category,
                actionId = actionId,
                displayName = displayName,
                inputActionAsset = asset,
                actionMapName = map,
                actionName = action,
                bindingIndex = bindingIndex,
                isCompositeBinding = composite,
                compositePartName = compositePart
            };
        }

        static void CreateSceneEntry(string id, string fileName, string displayName)
        {
            var assetPath = $"{ScenesPath}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SceneEntry>(assetPath);
            if (existing != null) {
                existing.sceneFileName = fileName;
                existing.displayName = displayName;
                EditorUtility.SetDirty(existing);
                return;
            }
            var entry = ScriptableObject.CreateInstance<SceneEntry>();
            entry.id = $"scene.{id}";
            entry.entryName = fileName;
            entry.displayName = displayName;
            entry.sceneFileName = fileName;
            AssetDatabase.CreateAsset(entry, assetPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) { EnsureFolder(parent); }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
