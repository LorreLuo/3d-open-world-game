# SP0 工程地基 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立 `Assets/_Game` 工程地基：Demo 场景迁移为主世界、玩家换用 Spark 第三人称控制器、Spark 交互入口 + UIS 菜单桥接、主菜单/加载界面/设置面板（中文 UI）、PC 构建全链路可用且零编译错误。

**Architecture:** 所有自写代码放在 `Assets/_Game`（`Game.Runtime` / `Game.Bridge` / `Game.Editor` / `Game.Tests` 四个 asmdef，全部命名空间化）。场景/预制体/数据库资产一律由 `Game.Editor` 中的构建器脚本在 Unity batchmode 下确定性生成（可在命令行验证、可评审、可重跑）。`Game.Bridge` 是 Spark 与 UIS 唯一接触点。Unity 侧验证用 batchmode：编译检查、PlayMode 测试、PC 构建，由 `tools/unity.ps1` 封装。

**Tech Stack:** Unity 6000.4.5f1（URP 17.4.0）、Spark（Core/Rules/Triggers/Scenes/GameSettings/Interactables、ThirdPersonController）、Opsive UIS 1.3.8 + Shared 2.1.0、Cinemachine 3.1.4、Input System 1.19.0、TextMeshPro。

**Spec:** `docs/superpowers/specs/2026-08-20-spark-uis-rpg-demo-design.md`

## Global Constraints

- Unity **6000.4.5f1**；渲染管线 **URP 17.4.0**；目标平台 **PC (StandaloneWindows64)**。
- **禁止修改** `Assets/Blink/Spark` 与 `Packages/` 下任何文件（只读引用）。
- 所有新文件放在 `Assets/_Game/`、`docs/`、`tools/`；`Assets/Samples/.../Demo` 原目录只读（迁移用复制，不原地修改）。
- 自写 C# 全部带命名空间（`Game.Runtime.*`、`Game.Bridge.*`、`Game.Editor.*`、`Game.Tests`），禁止新增全局类型。
- 自建 UI 全部中文；不新建英文 UI。
- 现有 `Assets/scripts`（早期原型）与 `Assets/Scenes/SampleScene.unity` 不动（后者仅从构建列表移除）。
- Spark 数据库资产必须位于 `Resources` 下：统一放 `Assets/_Game/Data/Resources/`。
- 每次 Unity 批处理验证统一经 `tools/unity.ps1`；提交信息格式 `sp0: <内容>`。

**关键 GUID 速查（asmdef 引用用）：**

| 程序集 | GUID |
|---|---|
| Spark.Core | `00a66b3abbb477b42bd871e52fef35d5` |
| Spark.Rules | `90c4fd31193ddfd42bbd9a0343ab6953` |
| Spark.Triggers | `7b702ed6fdc641b46abf81d48b50e313` |
| Spark.Scenes | `58cafe6aa25e81840a5d0c1c17d73646` |
| Spark.GameSettings | `431c47b6ba2fcf24db428079881cdee1` |
| Spark.UI | `8db879d5e55d1184c9862909caf76fe4` |
| Spark.ThirdPersonController | `e3e4c9298a186394a962522fd1e1406e` |
| Spark.Interactables | `87b6ca17a960fac4cab63d31c340cac6` |
| Opsive.UltimateInventorySystem | `33948188067f67944b82252675cf09c3` |
| Opsive.Shared.Runtime | `d8e89a79cd8df884b8d5b3356783eb74` |
| Opsive.UltimateInventorySystem.Demo | `096d6c1262816c04ca09d7ae1d201d8a` |
| Unity.TextMeshPro | `6055be8ebefd69e48b49212b09b47b2f` |
| Unity.InputSystem | `75469ad4d38634e559750d17036d5f7c` |
| Unity.Cinemachine | `4307f53044263cf4b835bd812fc161a4` |

**关键资产路径：**

- 玩家源 prefab：`Assets/Samples/Opsive Ultimate Inventory System/1.3.8/Demo/Prefabs/Characters/Player/Player Character.prefab`
- 玩家源动画控制器：`Assets/Samples/Opsive Ultimate Inventory System/1.3.8/Demo/Animations/Character/Character.controller`
- Spark 第三人称输入：`Assets/Blink/Spark/Core/Runtime/CharacterControllers/ThirdPerson/ThirdPersonInput.inputactions`
- 交互输入（含 Interact 动作）：`Assets/InputSystem_Actions.inputactions`
- Spark 交互指示器 prefab：`Assets/Blink/Spark/Core/Plugins/UI/Runtime/UI Prefabs/InteractableIndicator.prefab`
- 键位行/分类标题 prefab：`Assets/Blink/Spark/Core/Plugins/GameSettings/Runtime/UI Prefabs/KeybindRow.prefab`、`.../KeybindCategoryTitle.prefab`

---

### Task 1: 分支与 Unity 验证基建

**Files:**
- Create: `tools/unity.ps1`
- Create: `Assets/_Game/Editor/Verification/ProjectVerifier.cs`
- Create: `Assets/_Game/Editor/Verification/Game.Editor.asmdef`（注意：本任务先建 Editor 目录与占位 asmdef，Task 3 会补全全部 asmdef；此处 Game.Editor 引用先给最小集：无 references，includePlatforms ["Editor"]）

**Interfaces:**
- Produces: `tools/unity.ps1 -Action <Compile|TestPlay|Build|VerifyTags>`（后续所有任务验证入口）；`Game.Editor.ProjectVerifier.CompileCheck/BuildWindows` 静态方法。

- [ ] **Step 1: 创建功能分支**

```powershell
git checkout -b feature/sp0-foundation
```

- [ ] **Step 2: 创建 `tools/unity.ps1`**

```powershell
param([Parameter(Mandatory=$true)][ValidateSet("Compile","TestPlay","Build","VerifyTags")][string]$Action)
$ErrorActionPreference = "Stop"
$proj = Split-Path -Parent $PSScriptRoot
$unity = $env:UNITY_PATH
if (-not $unity) {
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
        "C:\Program Files\Unity 6000.4.5f1\Editor\Unity.exe"
    )
    $unity = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $unity) {
        $found = Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Filter "Unity.exe" -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) { $unity = $found.FullName }
    }
}
if (-not $unity -or -not (Test-Path $unity)) { throw "未找到 Unity.exe，请设置 UNITY_PATH 环境变量" }
$log = Join-Path $env:TEMP ("unity-{0}-{1}.log" -f $Action, (Get-Date -Format "yyyyMMddHHmmss"))
$base = @("-batchmode","-nographics","-projectPath",$proj,"-logFile",$log,"-quit")
switch ($Action) {
    "Compile"    { & $unity @base "-executeMethod" "Game.Editor.ProjectVerifier.CompileCheck" | Out-Null }
    "VerifyTags" { & $unity @base "-executeMethod" "Game.Editor.TagManagerUpdater.Setup" | Out-Null }
    "TestPlay"   { & $unity "-batchmode","-nographics","-projectPath",$proj,"-logFile",$log,"-runTests","-testPlatform","PlayMode","-testResults",(Join-Path $proj "TestResults/playmode.xml") | Out-Null }
    "Build"      { & $unity @base "-executeMethod" "Game.Editor.ProjectVerifier.BuildWindows" | Out-Null }
}
$exit = $LASTEXITCODE
$content = if (Test-Path $log) { Get-Content $log -Raw } else { "" }
if ($Action -eq "Compile" -and $content -notmatch "COMPILE_CHECK_DONE") { throw "编译检查未完成（存在编译错误或方法未执行）。日志: $log" }
if ($content -match "error CS") { throw "存在编译错误。日志: $log" }
if ($Action -eq "Build") {
    if ($content -notmatch "BUILD_RESULT:") { throw "构建结果未输出。日志: $log" }
    if ($content -notmatch "BUILD_RESULT: Succeeded") { throw "构建失败。日志: $log" }
}
if ($exit -ne 0) { throw "Unity 退出码 $exit。日志: $log" }
Write-Host "OK: $Action 通过 ($log)"
```

- [ ] **Step 3: 创建 `Assets/_Game/Editor/Verification/ProjectVerifier.cs`**

```csharp
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static class ProjectVerifier
    {
        [MenuItem("Game/Verify/Compile Check")]
        public static void CompileCheck()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("COMPILE_CHECK_DONE");
        }

        [MenuItem("Game/Verify/Build Windows Player")]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var outDir = "Builds/SparkUISDemo";
            Directory.CreateDirectory(outDir);
            var report = BuildPipeline.BuildPlayer(scenes, outDir + "/SparkUISDemo.exe",
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log("BUILD_RESULT: " + report.summary.result + " totalErrors=" + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) { EditorApplication.Exit(1); }
        }
    }
}
```

- [ ] **Step 4: 创建 `Assets/_Game/Editor/Verification/Game.Editor.asmdef`**

```json
{
    "name": "Game.Editor",
    "rootNamespace": "Game.Editor",
    "references": [],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: 验证基线编译通过**

Run: `pwsh -File tools/unity.ps1 -Action Compile`
Expected: `OK: Compile 通过`（这是改造前基线；若原本就有编译错误，先报告，不继续）

- [ ] **Step 6: 提交**

```powershell
git add tools/unity.ps1 Assets/_Game/Editor/
git commit -m "sp0: Unity 批处理验证基建（unity.ps1 + ProjectVerifier）"
```

---

### Task 2: 工程标签与层配置

**Files:**
- Create: `Assets/_Game/Editor/Config/TagManagerUpdater.cs`
- Modify: `ProjectSettings/TagManager.asset`（经脚本修改）

**Interfaces:**
- Produces: 标签 `Player`、`MainCamera`；层 `Player`（`LayerMask.NameToLayer("Player")` 与 `LayerMask.NameToLayer("Ground")` 可用）。

- [ ] **Step 1: 创建 `Assets/_Game/Editor/Config/TagManagerUpdater.cs`**

```csharp
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class TagManagerUpdater
    {
        [MenuItem("Game/Setup/Tags and Layers")]
        public static void Setup()
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var so = new SerializedObject(tagManager);
            AddToStringArray(so, "tags", "Player");
            AddToStringArray(so, "tags", "MainCamera");
            AddToStringArray(so, "layers", "Player");
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("TAGS_SETUP_DONE");
        }

        static void AddToStringArray(SerializedObject so, string property, string value)
        {
            var prop = so.FindProperty(property);
            for (int i = 0; i < prop.arraySize; i++) {
                if (prop.GetArrayElementAtIndex(i).stringValue == value) { return; }
            }
            for (int i = 0; i < prop.arraySize; i++) {
                var el = prop.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(el.stringValue)) { el.stringValue = value; return; }
            }
            prop.InsertArrayElementAtIndex(prop.arraySize);
            prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = value;
        }
    }
}
```

- [ ] **Step 2: 执行并验证**

Run: `pwsh -File tools/unity.ps1 -Action VerifyTags`
Expected: `OK: VerifyTags 通过`（日志含 `TAGS_SETUP_DONE`）

- [ ] **Step 3: 检查结果**

Run: `git diff -- ProjectSettings/TagManager.asset`
Expected: 新增 `Player`、`MainCamera` 两个 tag；`layers` 中第一个空槽填入 `Player`。注意 Unity 可能同时重排/保存其它内容，若 diff 仅含上述新增即正常。

- [ ] **Step 4: 提交**

```powershell
git add ProjectSettings/TagManager.asset Assets/_Game/Editor/Config/
git commit -m "sp0: 添加 Player/MainCamera 标签与 Player 层"
```

---

### Task 3: _Game 程序集骨架（4 个 asmdef + 命名空间）

**Files:**
- Create: `Assets/_Game/Runtime/Game.Runtime.asmdef`
- Create: `Assets/_Game/Bridge/Game.Bridge.asmdef`
- Modify: `Assets/_Game/Editor/Verification/Game.Editor.asmdef`（替换内容，移动到 `Assets/_Game/Editor/Game.Editor.asmdef`，删除旧文件）
- Create: `Assets/_Game/Tests/Game.Tests.asmdef`

**Interfaces:**
- Produces: 程序集 `Game.Runtime`（游戏核心，可引用单个框架）、`Game.Bridge`（唯一可同时引用 Spark+UIS 的程序集）、`Game.Editor`（编辑器工具，includePlatforms Editor）、`Game.Tests`（PlayMode/EditMode 测试，defineConstraints UNITY_INCLUDE_TESTS）。

- [ ] **Step 1: 创建 `Assets/_Game/Runtime/Game.Runtime.asmdef`**

```json
{
    "name": "Game.Runtime",
    "rootNamespace": "Game.Runtime",
    "references": [
        "GUID:00a66b3abbb477b42bd871e52fef35d5",
        "GUID:58cafe6aa25e81840a5d0c1c17d73646",
        "GUID:431c47b6ba2fcf24db428079881cdee1",
        "GUID:33948188067f67944b82252675cf09c3",
        "GUID:d8e89a79cd8df884b8d5b3356783eb74",
        "GUID:096d6c1262816c04ca09d7ae1d201d8a"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: 创建 `Assets/_Game/Bridge/Game.Bridge.asmdef`**

```json
{
    "name": "Game.Bridge",
    "rootNamespace": "Game.Bridge",
    "references": [
        "GUID:00a66b3abbb477b42bd871e52fef35d5",
        "GUID:7b702ed6fdc641b46abf81d48b50e313",
        "GUID:87b6ca17a960fac4cab63d31c340cac6",
        "GUID:33948188067f67944b82252675cf09c3",
        "GUID:d8e89a79cd8df884b8d5b3356783eb74",
        "GUID:096d6c1262816c04ca09d7ae1d201d8a"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: 创建 `Assets/_Game/Editor/Game.Editor.asmdef` 并删除 `Assets/_Game/Editor/Verification/Game.Editor.asmdef`（同时把 ProjectVerifier.cs 移入 `Assets/_Game/Editor/Verification/`，保持路径不变即可）**

```json
{
    "name": "Game.Editor",
    "rootNamespace": "Game.Editor",
    "references": [
        "GUID:00a66b3abbb477b42bd871e52fef35d5",
        "GUID:90c4fd31193ddfd42bbd9a0343ab6953",
        "GUID:7b702ed6fdc641b46abf81d48b50e313",
        "GUID:58cafe6aa25e81840a5d0c1c17d73646",
        "GUID:431c47b6ba2fcf24db428079881cdee1",
        "GUID:8db879d5e55d1184c9862909caf76fe4",
        "GUID:87b6ca17a960fac4cab63d31c340cac6",
        "GUID:e3e4c9298a186394a962522fd1e1406e",
        "GUID:4307f53044263cf4b835bd812fc161a4",
        "GUID:6055be8ebefd69e48b49212b09b47b2f",
        "GUID:75469ad4d38634e559750d17036d5f7c",
        "GUID:33948188067f67944b82252675cf09c3",
        "GUID:d8e89a79cd8df884b8d5b3356783eb74",
        "GUID:096d6c1262816c04ca09d7ae1d201d8a",
        "GUID:8f88b081f5b9b0f4f9e2c61dcb4b8b04",
        "GUID:0f679c4f1f27b534f83c0a93c2c44a1a"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

> 注：末尾两个 GUID 是 `Game.Runtime` 与 `Game.Bridge` 自身 asmdef 的 GUID，创建后从各自的 `.meta` 文件读取并把 `GUID:8f88...`/`GUID:0f67...` 替换为真实值（若与真实值不同，以 `.meta` 为准）。

- [ ] **Step 4: 创建 `Assets/_Game/Tests/Game.Tests.asmdef`**

```json
{
    "name": "Game.Tests",
    "rootNamespace": "Game.Tests",
    "references": [
        "GUID:00a66b3abbb477b42bd871e52fef35d5",
        "GUID:58cafe6aa25e81840a5d0c1c17d73646",
        "GUID:90c4fd31193ddfd42bbd9a0343ab6953",
        "GUID:431c47b6ba2fcf24db428079881cdee1",
        "GUID:e3e4c9298a186394a962522fd1e1406e",
        "GUID:33948188067f67944b82252675cf09c3",
        "GUID:096d6c1262816c04ca09d7ae1d201d8a",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

> 注：第一个引用位置同样需要加入 `Game.Runtime` 与 `Game.Bridge` 的真实 GUID（从两个 asmdef 的 `.meta` 读取后替换）。

- [ ] **Step 5: 验证编译**

Run: `pwsh -File tools/unity.ps1 -Action Compile`
Expected: `OK: Compile 通过`

- [ ] **Step 6: 提交**

```powershell
git add Assets/_Game/
git commit -m "sp0: _Game 四个程序集骨架与命名空间边界"
```

---

### Task 4: Spark 数据库基础资产生成器

**Files:**
- Create: `Assets/_Game/Editor/DataAssets/DatabaseGenerator.cs`

**Interfaces:**
- Produces（幂等，可重复执行）：
  - `Assets/_Game/Data/Resources/Database/Rules/<KEY>.asset`（`RuleEntry`，key=defaultValue：`MOVEMENT=true`、`JUMPING=true`、`CAMERA_CONTROLS=true`、`GROUNDED=false`、`DEAD=false`、`TARGET_LOCKED=false`、`UI_OPENED=false`、`ROOT_MOTION=false`、`IMMUNE=false`、`IN_DODGE_ROLL=false`、`MOTION_WARP=false`、`IN_COMBAT=false`；id=`rule.<KEY>`）
  - `Assets/_Game/Data/Resources/Database/Scenes/MainMenu.asset`（`SceneEntry`，id=`scene.MainMenu`，sceneFileName=`MainMenu`）
  - `Assets/_Game/Data/Resources/Database/Scenes/GameWorld.asset`（`SceneEntry`，id=`scene.GameWorld`，sceneFileName=`GameWorld`，defaultSpawnPosition 由 Task 10 迁移器回填）
  - `Assets/_Game/Data/Resources/Settings/GeneralKeybindCategory.asset`（`KeybindCategoryEntry`，id=`keybind.general`，displayName=`通用`）
  - `Assets/_Game/Data/Resources/Settings/GameSettingsPluginSettings.asset`（inputActionAssets=[ThirdPersonInput, InputSystem_Actions]；keybindActions：move/移动、jump/跳跃、sprint/冲刺、roll/翻滚 → ThirdPersonInput 的 `Player` 映射；interact/交互 → InputSystem_Actions 的 `Player/Interact`；cursor/光标 → ThirdPersonInput 的 `Player/EnableCursor`）

- [ ] **Step 1: 创建 `Assets/_Game/Editor/DataAssets/DatabaseGenerator.cs`**

```csharp
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
```

- [ ] **Step 2: 执行生成**

Run: `pwsh -File tools/unity.ps1 -Action Compile` 先确认编译（新增脚本后）；然后执行生成需要临时在 Compile 后调用。用下面命令直接在 batchmode 里调生成：

```powershell
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" }
& $unity -batchmode -nographics -projectPath "." -logFile "$env:TEMP\unity-gen.log" -quit -executeMethod "Game.Editor.DatabaseGenerator.Generate"
```

Expected: 日志含 `DATABASE_GENERATE_DONE`，且以下文件存在：

```powershell
Test-Path "Assets/_Game/Data/Resources/Database/Rules/MOVEMENT.asset"   # True
Test-Path "Assets/_Game/Data/Resources/Database/Scenes/GameWorld.asset" # True
Test-Path "Assets/_Game/Data/Resources/Settings/GameSettingsPluginSettings.asset" # True
```

- [ ] **Step 3: 提交**

```powershell
git add Assets/_Game/Editor/DataAssets/ Assets/_Game/Data/
git commit -m "sp0: Spark 数据库基础资产（规则/场景/键位设置）生成器"
```

---

### Task 5: 玩家运行时替代脚本（GamePlayerCharacter + GamePlayerDamageable）

**Files:**
- Create: `Assets/_Game/Runtime/Player/GamePlayerCharacter.cs`
- Create: `Assets/_Game/Runtime/Player/GamePlayerDamageable.cs`

**Interfaces:**
- Consumes: Demo 程序集类型 `Stats`/`CharacterStats`/`CharacterAnimator`/`DamagePopupSpawner`/`Flash`/`ItemHotbar`；`Opsive.Shared.Game.Scheduler`。
- Produces: `Game.Runtime.Player.GamePlayerCharacter`（`CharacterStats`、`CharacterAnimator`、`Damageable`、`Inventory`、`ItemHotbar` 属性；`Die()`/`Respawn()`）；`Game.Runtime.Player.GamePlayerDamageable : Damageable`（被 `HpMonitor` 与 `DamagePopupSpawner` 消费）。

- [ ] **Step 1: 创建 `Assets/_Game/Runtime/Player/GamePlayerCharacter.cs`**

```csharp
using Opsive.Shared.Game;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Demo.CharacterControl;
using Opsive.UltimateInventorySystem.Demo.Damageable;
using Opsive.UltimateInventorySystem.Equipping;
using Opsive.UltimateInventorySystem.ItemActions;
using Opsive.UltimateInventorySystem.UI.Panels.Hotbar;
using UnityEngine;

namespace Game.Runtime.Player
{
    /// <summary>
    /// Spark 控制器时代的玩家角色：替代 UIS Demo 的 Character/PlayerCharacter。
    /// 移除旧移动/旋转/相机/输入逻辑，保留属性、动画、死亡重生、快捷栏与伤害飘字注册。
    /// </summary>
    public class GamePlayerCharacter : MonoBehaviour
    {
        [Tooltip("角色基础属性。")]
        [SerializeField] protected Stats m_BaseStats;
        [Tooltip("死亡后是否重生。")]
        [SerializeField] protected bool m_RespawnOnDeath = true;
        [Tooltip("重生位置（世界坐标）。")]
        [SerializeField] protected Vector3 m_RespawnPosition = new Vector3(0, 1, 0);
        [Tooltip("物品快捷栏。")]
        [SerializeField] protected ItemHotbar m_ItemHotbar;

        protected Animator m_Anim;
        protected Inventory m_Inventory;
        protected IEquipper m_Equipper;
        protected ItemUser m_ItemUser;
        protected GamePlayerDamageable m_Damageable;
        protected CharacterStats m_CharacterStats;
        protected CharacterAnimator m_CharacterAnimator;

        public CharacterStats CharacterStats => m_CharacterStats;
        public CharacterAnimator CharacterAnimator => m_CharacterAnimator;
        public GamePlayerDamageable Damageable => m_Damageable;
        public Inventory Inventory => m_Inventory;
        public ItemUser ItemUser => m_ItemUser;
        public ItemHotbar ItemHotbar => m_ItemHotbar;

        protected virtual void Awake()
        {
            m_Anim = GetComponent<Animator>();
            m_Inventory = GetComponent<Inventory>();
            m_Equipper = GetComponent<IEquipper>();
            m_ItemUser = GetComponent<ItemUser>();
            m_Damageable = GetComponent<GamePlayerDamageable>();
            m_CharacterStats = new CharacterStats(m_BaseStats, m_Equipper);
            m_CharacterAnimator = new CharacterAnimator(m_Anim);
            Physics.IgnoreLayerCollision(8, 10);
        }

        protected virtual void Start()
        {
            DamagePopupSpawner.RegisterDamageable(m_Damageable, DamagePopupSpawner.DamageableType.PLAYER);
            if (m_ItemHotbar == null) {
#if UNITY_6000_5_OR_NEWER
                m_ItemHotbar = FindAnyObjectByType<ItemHotbar>();
#else
                m_ItemHotbar = FindObjectOfType<ItemHotbar>();
#endif
            }
        }

        public virtual void Die()
        {
            gameObject.SetActive(false);
            if (m_RespawnOnDeath) { Scheduler.Schedule(0.5f, Respawn); }
        }

        public virtual void Respawn()
        {
            m_Damageable.Heal(int.MaxValue, false);
            transform.position = m_RespawnPosition;
            gameObject.SetActive(true);
        }

        protected virtual void OnDestroy()
        {
            DamagePopupSpawner.UnregisterDamageable(m_Damageable, DamagePopupSpawner.DamageableType.PLAYER);
        }
    }
}
```

- [ ] **Step 2: 创建 `Assets/_Game/Runtime/Player/GamePlayerDamageable.cs`**

```csharp
using Opsive.UltimateInventorySystem.Demo.Damageable;
using Opsive.UltimateInventorySystem.Demo.Events;
using UnityEngine;
using EventHandler = Opsive.Shared.Events.EventHandler;
using Random = UnityEngine.Random;

namespace Game.Runtime.Player
{
    /// <summary>
    /// 玩家可受伤组件：与 Demo 的 DemoCharacterDamageable 等价，但引用 GamePlayerCharacter。
    /// </summary>
    public class GamePlayerDamageable : Damageable, IDamageable
    {
        [Tooltip("关联的玩家角色。")]
        [SerializeField] protected GamePlayerCharacter m_Character;
        [Tooltip("受击闪烁效果。")]
        [SerializeField] protected Flash m_Flash;

        public override int MaxHp => m_Character.CharacterStats.MaxHp;

        private void OnEnable()
        {
            if (m_Flash != null) { m_Flash.Reset(); }
        }

        public override void TakeDamage(int amount)
        {
            amount -= (int)(m_Character.CharacterStats.Defense * Random.Range(0.9f, 1.1f));
            base.TakeDamage(amount);
            m_Character.CharacterAnimator.Damaged();
            if (gameObject.activeInHierarchy == false) { return; }
            if (m_Flash != null) {
                StartCoroutine(m_Flash.CoroutineIE(Mathf.Clamp(m_InvincibilityTime, 0.4f, 1f)));
            }
        }

        public override void Die()
        {
            m_Character.Die();
            m_Character.CharacterAnimator.Die();
            EventHandler.ExecuteEvent(this, DemoEventNames.c_Damageable_OnDie_Damageable, this);
        }

        private void OnDisable()
        {
            if (m_Flash != null) { m_Flash.Reset(); }
        }
    }
}
```

- [ ] **Step 3: 验证编译**

Run: `pwsh -File tools/unity.ps1 -Action Compile`
Expected: `OK: Compile 通过`。若报错，常见修正：`m_InvincibilityTime`/`Heal` 的可见性（在 Demo 的 `Damageable` 基类中为 protected/public）——按报错调整，不允许删除功能。

- [ ] **Step 4: 提交**

```powershell
git add Assets/_Game/Runtime/
git commit -m "sp0: 玩家角色与可受伤组件（替代 Demo Character 链）"
```

---

### Task 6: UI 预制体与相机 Rig 生成器（中文字体/相机/LoadingScreen/InteractionCanvas）

**Files:**
- Create: `Assets/_Game/Editor/Prefabs/UiPrefabBuilder.cs`

**Interfaces:**
- Produces:
  - `Assets/_Game/Fonts/SimHei SDF.asset`（`TMP_FontAsset`，中文可用；来源：项目内现有中文字体，否则复制 `C:\Windows\Fonts\simhei.ttf` 进项目后生成；若两个都没有则抛异常报告）
  - `Assets/_Game/Prefabs/Camera/PlayerCameraRig.prefab`（Player Camera[MainCamera 标签, Camera+AudioListener+CinemachineBrain] → PlayerFollowCamera[CinemachineCamera+CinemachineThirdPersonFollow]）
  - `Assets/_Game/Prefabs/UI/LoadingScreen.prefab`（Canvas[sortOrder 999]+CanvasGroup+背景/渐隐/场景名/进度条/百分比/提示/点击提示+`LoadingScreenManager`，中文提示 3 条）
  - `Assets/_Game/Prefabs/UI/InteractionCanvas.prefab`（Canvas[sortOrder 500]+`InteractablesManager`+InteractableKey 层级，indicatorPrefab=Spark 的 InteractableIndicator.prefab）

- [ ] **Step 1: 创建 `Assets/_Game/Editor/Prefabs/UiPrefabBuilder.cs`**

```csharp
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
                var n = Path.GetFileName(p).ToLowerInvariant();
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
```

> 注：`SavePrefab` 两种分支等价（`SaveAsPrefabAsset` 自动覆盖），保留以防 Unity 6 行为差异。

- [ ] **Step 2: 编译并执行生成**

```powershell
pwsh -File tools/unity.ps1 -Action Compile
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" }
& $unity -batchmode -nographics -projectPath "." -logFile "$env:TEMP\unity-uiprefab.log" -quit -executeMethod "Game.Editor.UiPrefabBuilder.BuildAll"
```

Expected: 日志含 `UI_PREFABS_DONE`；4 个产物路径存在；`SimHei SDF.asset` 存在。

- [ ] **Step 3: 提交**

```powershell
git add Assets/_Game/Editor/Prefabs/ Assets/_Game/Prefabs/ Assets/_Game/Fonts/
git commit -m "sp0: 中文字体/相机Rig/LoadingScreen/InteractionCanvas 预制体"
```

---

### Task 7: 玩家预制体重建器（换 Spark 控制器）

**Files:**
- Create: `Assets/_Game/Editor/Prefabs/PlayerPrefabBuilder.cs`

**Interfaces:**
- Consumes: Task 6 的字体无关；`TagManagerUpdater`（Task 2）的标签；Demo 玩家 prefab 与 `Character.controller`；`ThirdPersonInput.inputactions`、`InputSystem_Actions.inputactions`。
- Produces: `Assets/_Game/Prefabs/Player/PlayerCharacter.prefab`（根：tag=`Player`；组件：SparkEntity+RulesEntity+SparkThirdPersonController[cameraFollowTarget=CameraFollowTarget, groundLayerMask=Ground]+PlayerInput[SendMessages, ThirdPersonInput]+InteractorEntity[InputSystem_Actions]+GamePlayerCharacter[迁移 m_BaseStats/m_RespawnOnDeath/m_ItemHotbar]+GamePlayerDamageable[迁移 m_Flash]+原有 Inventory/InventoryIdentifier/Equipper/ItemUser/InventoryInteractor/InventorySaver/CurrencyOwnerSaver/CharacterController/Rigidbody/Animator[新控制器]/UnityInputSystem/PlayerInputProxy/BillboardFX；已移除：PlayerCharacter、DemoCharacterDamageable、CharacterCamera）；子物体 `CameraFollowTarget`（局部 (0,1.5,0)）；`Assets/_Game/Animations/Player/PlayerAnimator.controller`（原控制器副本 + Spark 全部参数）。

- [ ] **Step 1: 创建 `Assets/_Game/Editor/Prefabs/PlayerPrefabBuilder.cs`**

```csharp
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
            var baseStats = sourceCharacterSo.FindProperty("m_BaseStats").objectReferenceValue;
            var respawnOnDeath = sourceCharacterSo.FindProperty("m_RespawnOnDeath").boolValue;
            var itemHotbar = sourceCharacterSo.FindProperty("m_ItemHotbar").objectReferenceValue;
            var sourceDamageableSo = new SerializedObject(sourceRoot.GetComponent<DemoCharacterDamageable>());
            var flash = sourceDamageableSo.FindProperty("m_Flash").objectReferenceValue;
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

            var playerInput = root.AddComponent<PlayerInput>();
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
            gpcSo.FindProperty("m_BaseStats").objectReferenceValue = baseStats;
            gpcSo.FindProperty("m_RespawnOnDeath").boolValue = respawnOnDeath;
            gpcSo.FindProperty("m_ItemHotbar").objectReferenceValue = itemHotbar;
            gpcSo.ApplyModifiedPropertiesWithoutUndo();

            var gpd = root.AddComponent<GamePlayerDamageable>();
            var gpdSo = new SerializedObject(gpd);
            gpdSo.FindProperty("m_Character").objectReferenceValue = gpc;
            gpdSo.FindProperty("m_Flash").objectReferenceValue = flash;
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
```

- [ ] **Step 2: 编译并执行**

```powershell
pwsh -File tools/unity.ps1 -Action Compile
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" }
& $unity -batchmode -nographics -projectPath "." -logFile "$env:TEMP\unity-playerprefab.log" -quit -executeMethod "Game.Editor.PlayerPrefabBuilder.Build"
```

Expected: 日志含 `PLAYER_PREFAB_DONE`；`Assets/_Game/Prefabs/Player/PlayerCharacter.prefab` 与 `Assets/_Game/Animations/Player/PlayerAnimator.controller` 存在。

- [ ] **Step 3: 检查 prefab 组件（防呆验证）**

Run:

```powershell
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" }
& $unity -batchmode -nographics -projectPath "." -logFile "$env:TEMP\unity-verify-prefab.log" -quit -executeMethod "Game.Editor.ProjectVerifier.CompileCheck"
```

然后打开编辑器人工复核（执行者若是 CLI 环境，则改为 grep prefab YAML）：
Expected: `Player Character.prefab` 的 YAML 中同时出现 `SparkThirdPersonController`、`PlayerInput`、`InteractorEntity`、`GamePlayerCharacter`、`GamePlayerDamageable`、`SparkEntity`、`RulesEntity` 的 m_Script 行，且不再出现 `PlayerCharacter`（Demo 版）与 `DemoCharacterDamageable`。

- [ ] **Step 4: 提交**

```powershell
git add Assets/_Game/Editor/Prefabs/PlayerPrefabBuilder.cs Assets/_Game/Prefabs/Player/ Assets/_Game/Animations/Player/
git commit -m "sp0: 玩家预制体重建为 Spark 第三人称控制器栈"
```

---

### Task 8: 桥接层脚本（Game.Bridge）

**Files:**
- Create: `Assets/_Game/Bridge/UisMenuBridge.cs`
- Create: `Assets/_Game/Bridge/GateDoorBridge.cs`
- Create: `Assets/_Game/Bridge/UisPanelTrigger.cs`（含 `UisPanelTriggerDataAsset` + `UisPanelTriggerType`）

**Interfaces:**
- Consumes: Spark 全局类型（`SparkEntityRegistry`/`TriggerTypeBase`/`TriggerDataAsset`/`TriggerExecutionContext`）；UIS 类型（`InventoryPanelOpener`/`PanelOpener`/`Chest`[ns `Opsive.UltimateInventorySystem.UI.Menus.Chest`]/`InventorySystemManager`/`DynamicItemDefinition`[ns `Opsive.UltimateInventorySystem.Storage`]）；Demo 类型（`TextPanel`、`Inventory` 的 `MainItemCollection`）。
- Produces:
  - `Game.Bridge.UisMenuBridge.Open()`（无参；内部经 `SparkEntityRegistry.GetPlayerEntity()` 取玩家库存）
  - `Game.Bridge.GateDoorBridge.Open()`
  - `Game.Bridge.UisPanelTriggerType`（数据字段 `panelName`/`toggle`）

- [ ] **Step 1: 创建 `Assets/_Game/Bridge/UisMenuBridge.cs`**

```csharp
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.UI.Menus.Chest;
using Opsive.UltimateInventorySystem.UI.Panels;
using UnityEngine;

namespace Game.Bridge
{
    /// <summary>
    /// Spark 交互 → UIS 菜单 的桥接：交互物触发后打开对应的 UIS 面板并绑定库存。
    /// 挂在商店/制作台/储物屋/强化台/宝箱的交互物根物体上，由 InteractableObjectEntity.onInteract 调用 Open()。
    /// </summary>
    public class UisMenuBridge : MonoBehaviour
    {
        [Tooltip("需要绑定玩家库存的面板开启器（商店/制作台/储物屋）。")]
        [SerializeField] protected InventoryPanelOpener m_InventoryPanelOpener;
        [Tooltip("无需库存参数的面板开启器（强化台）。")]
        [SerializeField] protected PanelOpener m_PanelOpener;
        [Tooltip("宝箱组件（走 Chest.Open(玩家库存)）。")]
        [SerializeField] protected Chest m_Chest;

        public void Open()
        {
            var playerEntity = SparkEntityRegistry.GetPlayerEntity();
            if (playerEntity == null) {
                Debug.LogWarning("[Game.Bridge] UisMenuBridge: 未找到玩家实体，无法打开菜单。", this);
                return;
            }
            var inventory = playerEntity.GetComponent<Inventory>();
            if (m_InventoryPanelOpener != null && inventory != null) {
                m_InventoryPanelOpener.Open(inventory);
            } else if (m_Chest != null && inventory != null) {
                m_Chest.Open(inventory);
            } else if (m_PanelOpener != null) {
                m_PanelOpener.Open();
            }
        }
    }
}
```

- [ ] **Step 2: 创建 `Assets/_Game/Bridge/GateDoorBridge.cs`**

```csharp
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Demo.UI;
using Opsive.UltimateInventorySystem.Storage;
using UnityEngine;

namespace Game.Bridge
{
    /// <summary>
    /// 大门交互（Spark 交互入口版）：有钥匙开门，无钥匙提示。逻辑平移自 Demo 的 GateDoor。
    /// </summary>
    public class GateDoorBridge : MonoBehaviour
    {
        [Tooltip("开门所需的钥匙物品定义。")]
        [SerializeField] protected DynamicItemDefinition m_GateKey;
        [Tooltip("大门动画控制器。")]
        [SerializeField] protected Animator m_Animator;
        [Tooltip("提示文本面板。")]
        [SerializeField] protected TextPanel m_TextPanel;
        [Tooltip("没有钥匙时显示的文本。")]
        [SerializeField] protected string m_TextIfNoKey = "需要大门钥匙。";
        [Tooltip("有钥匙时显示的文本。")]
        [SerializeField] protected string m_TextHasKey = "大门已打开。";
        [Tooltip("文本显示时长（秒）。")]
        [SerializeField] protected float m_TextDisplayTime = 5f;

        private static readonly int s_Open = Animator.StringToHash("Open");
        protected bool m_DoorOpened;

        public void Open()
        {
            if (m_DoorOpened) { return; }

            var playerEntity = SparkEntityRegistry.GetPlayerEntity();
            if (playerEntity == null) { return; }
            var inventory = playerEntity.GetComponent<Inventory>();
            if (inventory == null) { return; }

            if (inventory.MainItemCollection.HasItem((1, m_GateKey), false)) {
                m_Animator.SetTrigger(s_Open);
                m_DoorOpened = true;
                if (m_TextPanel != null) { m_TextPanel.DisplayText(m_TextHasKey, m_TextDisplayTime); }
            } else {
                if (m_TextPanel != null) { m_TextPanel.DisplayText(m_TextIfNoKey, m_TextDisplayTime); }
            }
        }
    }
}
```

- [ ] **Step 3: 创建 `Assets/_Game/Bridge/UisPanelTrigger.cs`**

```csharp
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.UI.Panels;
using System;
using UnityEngine;

namespace Game.Bridge
{
    /// <summary>
    /// 按面板名打开/关闭 UIS 面板的 Spark 触发器数据。
    /// </summary>
    public class UisPanelTriggerDataAsset : TriggerDataAsset
    {
        [Tooltip("UIS DisplayPanel 唯一名，例如 \"Main Menu\"。")]
        public string panelName = "";
        [Tooltip("true=切换开关；false=直接打开。")]
        public bool toggle;
    }

    /// <summary>
    /// Spark 触发器类型：按名字操作 UIS 面板（SP2 暂停菜单/UI 按钮将复用）。
    /// </summary>
    public class UisPanelTriggerType : TriggerTypeBase
    {
        public override Type GetExpectedDataType()
        {
            return typeof(UisPanelTriggerDataAsset);
        }

        public override bool CanExecute(TriggerExecutionContext context)
        {
            return base.CanExecute(context) && InventorySystemManager.GetDisplayPanelManager(1) != null;
        }

        public override void Execute(TriggerExecutionContext context)
        {
            var data = context.TriggerEntry != null
                ? context.TriggerEntry.GetTriggerData<UisPanelTriggerDataAsset>()
                : null;
            if (data == null) { data = GetData<UisPanelTriggerDataAsset>(); }
            if (data == null || string.IsNullOrEmpty(data.panelName)) {
                Debug.LogWarning("[Game.Bridge] UisPanelTriggerType: panelName 为空。");
                return;
            }
            var manager = InventorySystemManager.GetDisplayPanelManager(1);
            if (data.toggle) { manager.TogglePanel(data.panelName); }
            else { manager.OpenPanel(data.panelName); }
        }
    }
}
```

> 若编译报错 `GetTriggerData`/`GetData` 不存在，说明 TriggerTypeBase 的 API 名称不同：此时读 `Assets/Blink/Spark/Core/Plugins/Triggers/Runtime/Scripts/Types/TriggerTypeBase.cs` 找等效方法（如 `GetData<T>()` 或 `context.TriggerEntry.triggerTypeData`），改用实际 API，并保持语义不变。

- [ ] **Step 4: 验证编译**

Run: `pwsh -File tools/unity.ps1 -Action Compile`
Expected: `OK: Compile 通过`

- [ ] **Step 5: 提交**

```powershell
git add Assets/_Game/Bridge/
git commit -m "sp0: 桥接层（UisMenuBridge/GateDoorBridge/UisPanelTriggerType）"
```

---

### Task 9: 主菜单场景构建器（中文 UI + 设置面板 + 场景切换）

**Files:**
- Create: `Assets/_Game/Runtime/MainMenu/MainMenuFlow.cs`
- Create: `Assets/_Game/Editor/Scenes/MainMenuSceneBuilder.cs`

**Interfaces:**
- Consumes: Task 4 的 `GameWorld.asset`（SceneEntry）、Task 6 的 LoadingScreen 预制体与中文字体。
- Produces: `Assets/_Game/Scenes/MainMenu.unity`（含：Main Camera[MainCamera 标签]、EventSystem[InputSystemUIInputModule]、MainMenuCanvas[CanvasScaler 1920×1080]、主菜单面板[标题"开放世界生存"+ 新游戏/继续游戏(禁用)/设置/退出]、GameSettings 面板[GameSettingsManager+GameSettingsUI+3 分类页签+VideoSettingsUI(6 TMP_Dropdown+垂直同步 Toggle)+AudioSettingsUI(4 Slider)+KeybindSettingsUI(KeybindRow/KeybindCategoryTitle 预制体+改绑遮罩+重置按钮)]、LoadingScreen 实例、`MainMenuFlow`[m_GameWorldSceneEntry=GameWorld.asset]）。

- [ ] **Step 1: 创建 `Assets/_Game/Runtime/MainMenu/MainMenuFlow.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.MainMenu
{
    /// <summary>
    /// 主菜单流程控制。新游戏经 Spark 场景加载器进入游戏世界；继续游戏在 SP2 接入。
    /// </summary>
    public class MainMenuFlow : MonoBehaviour
    {
        [Tooltip("游戏世界场景条目。")]
        [SerializeField] protected SceneEntry m_GameWorldSceneEntry;
        [Tooltip("继续游戏按钮（SP2 前禁用）。")]
        [SerializeField] protected Button m_ContinueButton;

        protected void Start()
        {
            if (m_ContinueButton != null) { m_ContinueButton.interactable = false; }
        }

        public void OnNewGame()
        {
            SceneLoader.LoadScene(m_GameWorldSceneEntry);
        }

        public void OnContinue()
        {
            Debug.Log("[Game] 继续游戏将在 SP2 存档系统接入后开放。");
        }

        public void OnSettings()
        {
            Spark.Network.ExecuteCommand(new OpenGameSettingsCommand(0));
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
```

- [ ] **Step 2: 创建 `Assets/_Game/Editor/Scenes/MainMenuSceneBuilder.cs`**

```csharp
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
            panelGo.transform.SetParent(settingsRoot.transform, false);
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
                var label = UiPrefabBuilder.CreateText(view, items[i].label + "Label", items[i].label, 28,
                    new Vector2(-420, 260 - i * 110), font, TextAlignmentOptions.Left);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
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

            so.FindProperty("rebindOverlay").objectReferenceValue = overlay.AddComponent<KeybindRebindOverlayUI>();
            var rebindSo = new SerializedObject(overlay.GetComponent<KeybindRebindOverlayUI>());
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
            var itemToggle = itemGo.AddComponent<Toggle>();
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
            text.alignment = TextAlignmentOptions.MiddleCenter;
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
```

- [ ] **Step 3: 编译并构建场景**

```powershell
pwsh -File tools/unity.ps1 -Action Compile
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" }
& $unity -batchmode -nographics -projectPath "." -logFile "$env:TEMP\unity-mainmenu.log" -quit -executeMethod "Game.Editor.MainMenuSceneBuilder.Build"
```

Expected: 日志含 `MAINMENU_SCENE_DONE`；`Assets/_Game/Scenes/MainMenu.unity` 存在。若编译报错：最常见是 `GameSettingsUI` 的 `GamePanel` 字段名、`VideoSettingsUI` 字段名、`CinemachineCamera.Priority` 属性名与本计划不同——按报错与实际类型成员修正名称（读对应源码文件确认），语义不变。

- [ ] **Step 4: 提交**

```powershell
git add Assets/_Game/Runtime/MainMenu/ Assets/_Game/Editor/Scenes/ Assets/_Game/Scenes/
git commit -m "sp0: 主菜单场景（中文UI/设置面板/场景切换入口）"
```

---

### Task 10: GameWorld 迁移器（Demo → 主世界 + 交互物改造）

**Files:**
- Create: `Assets/_Game/Editor/Scenes/GameWorldMigrator.cs`

**Interfaces:**
- Consumes: Task 7 的玩家 prefab、Task 6 的三个 prefab、Task 4 的 GameWorld SceneEntry。
- Produces: `Assets/_Game/Scenes/GameWorld.unity`（Demo 副本，完成：旧玩家实例替换为新 prefab 实例[位置/旋转保留]；旧 Main Camera[CharacterCamera] 移除并实例化 PlayerCameraRig；实例化 LoadingScreen 与 InteractionCanvas；Shop/CraftStable/StorageHouse/UpgradeStable/Chest(全部实例)/GateDoor 全部改为 Spark 交互入口[加 InteractableObjectEntity+桥接组件，移除 UIS Interactable 组件/子物体]，onInteract 已接到桥接 Open()；GameWorld SceneEntry 的 defaultSpawnPosition 回填为原玩家出生位置）。

- [ ] **Step 1: 创建 `Assets/_Game/Editor/Scenes/GameWorldMigrator.cs`**

```csharp
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
```

> 注：`ShopMenuOpener`/`CraftingMenuOpener` 位于命名空间 `Opsive.UltimateInventorySystem.UI.Menus`；`StorageMenuOpener` 位于 `Opsive.UltimateInventorySystem.Demo.Interaction.Interactables`（已核实）。`LockedChest : Chest`，会被 `FindObjectsByType<Chest>` 自动覆盖。

- [ ] **Step 2: 编译并执行迁移**

```powershell
pwsh -File tools/unity.ps1 -Action Compile
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" }
& $unity -batchmode -nographics -projectPath "." -logFile "$env:TEMP\unity-migrate.log" -quit -executeMethod "Game.Editor.GameWorldMigrator.Migrate"
```

Expected: 日志含 `GAMEWORLD_MIGRATE_DONE`；`Assets/_Game/Scenes/GameWorld.unity` 存在。**若日志出现任何 `error`（除编辑器无害警告外），停止并修复后重跑（迁移器幂等：重跑前可先删除 GameWorld.unity 再跑，或直接重跑——注意直接重跑会再次替换玩家/相机，因为场景内新玩家名为 "Player" 而查找的是 "Player Character"，不会重复；相机查找 "Main Camera" 会命中新 rig 的 "Player Camera"？不会，名字不同；但 InteractionCanvas/LoadingScreen 会重复实例化——因此重跑前先 `git checkout -- Assets/_Game/Scenes/GameWorld.unity` 恢复或删除该文件）**

- [ ] **Step 3: 人工复核场景（打开编辑器 GameWorld.unity）**

Expected：
- 场景内玩家名为 `Player`，根组件含 `SparkEntity`/`SparkThirdPersonController`/`GamePlayerCharacter`；
- `Shop` 根有 `InteractableObjectEntity`+`UisMenuBridge`，其下不再有 `Interactable` 子物体；`Chest`×3、`LockedChest` 同样改造（LockedChest 若为 Chest 组件变体则已被 RewireChests 覆盖；若无 Chest 组件，记录到备注，SP0 不处理锁定箱的开锁逻辑，仅其基础交互需保留——若改造后 LockedChest 无法交互，则手动为其加 InteractableObjectEntity+UisMenuBridge 指向 Chest 组件）；
- `GateDoor` 有 `InteractableObjectEntity`+`GateDoorBridge`；
- 场景内只有一台 `MainCamera` 标签相机（PlayerCameraRig 内）。

- [ ] **Step 4: 提交**

```powershell
git add Assets/_Game/Editor/Scenes/GameWorldMigrator.cs Assets/_Game/Scenes/GameWorld.unity Assets/_Game/Data/Resources/Database/Scenes/GameWorld.asset
git commit -m "sp0: Demo 场景迁移为 GameWorld + 交互物接入 Spark 交互入口"
```

---

### Task 11: PlayMode 测试套件

**Files:**
- Create: `Assets/_Game/Tests/Sp0SmokeTests.cs`

**Interfaces:**
- Consumes: 前面所有任务的产物（场景/预制体/数据库资产/桥接脚本）。
- Produces: PlayMode 测试（batchmode 可跑）：主菜单→世界切换、玩家 Spark 栈完整、移动规则存在、商店桥接开菜单。

- [ ] **Step 1: 创建 `Assets/_Game/Tests/Sp0SmokeTests.cs`**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.UI.Shop;
using Game.Bridge;
using Game.Runtime.Player;

namespace Game.Tests
{
    public class Sp0SmokeTests
    {
        [UnityTest]
        public IEnumerator MainMenu_Loads_And_Transitions_To_GameWorld()
        {
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return null;

            Assert.IsNotNull(GameObject.Find("MainMenuFlow"), "主菜单缺少 MainMenuFlow。");
            Assert.IsNotNull(LoadingScreenManager.Instance, "主菜单缺少 LoadingScreenManager。");
            var settingsUis = Object.FindObjectsByType<GameSettingsUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsTrue(settingsUis.Length > 0, "主菜单缺少 GameSettingsUI。");

            var world = Spark.GetPlugin<IScenesPlugin>().GetSceneEntry("scene.GameWorld");
            Assert.IsNotNull(world, "数据库缺少 scene.GameWorld 条目。");
            SceneLoader.LoadScene(world);

            var timeout = 30f;
            while (SceneManager.GetActiveScene().name != "GameWorld" && timeout > 0f) {
                timeout -= Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual("GameWorld", SceneManager.GetActiveScene().name, "未能在超时内切换到 GameWorld。");
        }

        [UnityTest]
        public IEnumerator GameWorld_Has_Player_With_Spark_Stack()
        {
            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;

            var player = SparkEntityRegistry.GetPlayerEntity();
            Assert.IsNotNull(player, "GameWorld 中找不到带 Player 标签的玩家实体。");
            Assert.IsNotNull(player.GetComponent<SparkThirdPersonController>(), "玩家缺少 SparkThirdPersonController。");
            Assert.IsNotNull(player.GetComponent<RulesEntity>(), "玩家缺少 RulesEntity。");
            Assert.IsNotNull(player.GetComponent<InteractorEntity>(), "玩家缺少 InteractorEntity。");
            Assert.IsNotNull(player.GetComponent<GamePlayerCharacter>(), "玩家缺少 GamePlayerCharacter。");
            Assert.IsNotNull(player.GetComponent<GamePlayerDamageable>(), "玩家缺少 GamePlayerDamageable。");
            Assert.IsNotNull(InteractablesManager.Instance, "场景缺少 InteractablesManager。");
        }

        [UnityTest]
        public IEnumerator Movement_Rules_Exist_With_Defaults()
        {
            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;

            Assert.IsTrue(SparkDatabaseRegistry.HasEntry("rule.MOVEMENT"), "缺少 rule.MOVEMENT。");
            Assert.IsTrue(SparkDatabaseRegistry.GetEntry<RuleEntry>("rule.MOVEMENT").defaultValue, "MOVEMENT 默认值应为 true。");
            Assert.IsTrue(SparkDatabaseRegistry.GetEntry<RuleEntry>("rule.JUMPING").defaultValue, "JUMPING 默认值应为 true。");
            Assert.IsTrue(SparkDatabaseRegistry.GetEntry<RuleEntry>("rule.CAMERA_CONTROLS").defaultValue, "CAMERA_CONTROLS 默认值应为 true。");

            var player = SparkEntityRegistry.GetPlayerEntity();
            Assert.IsNotNull(player, "找不到玩家实体。");
            Assert.IsNotNull(player.GetComponent<RulesEntity>(), "玩家缺少 RulesEntity。");
            var rulesPlugin = Spark.GetPlugin<IRulesPlugin>();
            Assert.IsTrue(rulesPlugin.GetRuleValue(player.gameObject, "MOVEMENT"), "玩家 MOVEMENT 规则应为 true。");
        }

        [UnityTest]
        public IEnumerator Shop_Bridge_Opens_Shop_Menu()
        {
            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;

            var bridges = Object.FindObjectsByType<UisMenuBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsTrue(bridges.Length >= 4, $"UisMenuBridge 数量不足（{bridges.Length}），交互物改造未完成。");

            UisMenuBridge shopBridge = null;
            foreach (var b in bridges) {
                if (b.GetComponent<ShopMenuOpener>() != null) { shopBridge = b; break; }
            }
            Assert.IsNotNull(shopBridge, "商店上缺少 UisMenuBridge。");

            shopBridge.Open();
            yield return null;

            var manager = InventorySystemManager.GetDisplayPanelManager(1);
            Assert.IsNotNull(manager, "找不到 DisplayPanelManager。");
            var panel = manager.GetPanel("Shop Menu");
            Assert.IsNotNull(panel, "不存在名为 'Shop Menu' 的面板。");
            Assert.AreEqual(panel, manager.SelectedDisplayPanel, "商店菜单未成为选中面板。");
        }
    }
}
```

- [ ] **Step 2: 配置构建场景（PlayMode 测试按场景名加载，必须先于测试配置）**

先在 `Assets/_Game/Editor/Verification/ProjectVerifier.cs` 追加方法：

```csharp
        [MenuItem("Game/Verify/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Game/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/_Game/Scenes/GameWorld.unity", true),
            };
            Debug.Log("BUILD_SETTINGS_DONE");
        }
```

执行：

```powershell
pwsh -File tools/unity.ps1 -Action Compile
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" }
& $unity -batchmode -nographics -projectPath "." -logFile "$env:TEMP\unity-buildsettings.log" -quit -executeMethod "Game.Editor.ProjectVerifier.ConfigureBuildSettings"
```

Expected: 日志含 `BUILD_SETTINGS_DONE`；`git diff ProjectSettings/EditorBuildSettings.asset` 只含两个新场景。

- [ ] **Step 3: 运行测试**

Run: `pwsh -File tools/unity.ps1 -Action TestPlay`
Expected: `OK: TestPlay 通过`（`TestResults/playmode.xml` 中 4 个用例全 Passed；若失败按断言信息修复对应产物，修复后重跑生成器/迁移器再重跑测试）。

- [ ] **Step 4: 提交**

```powershell
git add Assets/_Game/Tests/ TestResults/playmode.xml Assets/_Game/Editor/Verification/ProjectVerifier.cs ProjectSettings/EditorBuildSettings.asset
git commit -m "sp0: PlayMode 冒烟测试套件（场景切换/玩家栈/规则/商店桥接）"
```

---

### Task 12: PC 构建冒烟与收尾

**Files:**
- Modify: `ProjectSettings/EditorBuildSettings.asset`（已在 Task 11 配置）
- Produces: `Builds/SparkUISDemo/SparkUISDemo.exe`

**Interfaces:**
- Consumes: Task 11 的构建场景配置与全绿测试。

- [ ] **Step 1: PC 构建**

Run: `pwsh -File tools/unity.ps1 -Action Build`
Expected: `OK: Build 通过`；`Builds/SparkUISDemo/SparkUISDemo.exe` 存在。

- [ ] **Step 2: 全量回归**

```powershell
pwsh -File tools/unity.ps1 -Action Compile
pwsh -File tools/unity.ps1 -Action TestPlay
```

Expected: 全部 `OK`。

- [ ] **Step 3: 人工验收清单（打开编辑器逐一确认）**

1. 打开 MainMenu：标题"开放世界生存"、按钮中文、设置面板三页签可切换、视频下拉框有选项、音频滑条可拖、键位页有 6 行、改绑遮罩可弹出。
2. 点"新游戏"→ LoadingScreen 显示进度与中文提示 → 进入 GameWorld，玩家落地无穿地。
3. WASD 移动、鼠标视角、Space 跳、Shift 冲刺、Tab 释放光标、Ctrl 翻滚（动画正常）。
4. 靠近商店：出现指示器与 "E 商店" 提示 → 按 E → UIS 商店菜单打开；靠近宝箱/制作台/储物屋/强化台同理。
5. 拾取物（金币/物品）正常入包（UIS 交互保留）；Esc 可打开 UIS 主菜单。
6. 大门：无钥匙提示"需要大门钥匙。"；拿到钥匙后可开门。
7. 敌人会追击攻击玩家，玩家掉血、死亡后 0.5 秒重生（SP0 玩家主动攻击暂不可用，SP3 恢复）。
8. 控制台零报错（红色错误）。

- [ ] **Step 4: 提交并合并（合并 main 属共享分支操作，执行前先向用户确认）**

```powershell
git add -A
git commit -m "sp0: PC 构建冒烟通过（MainMenu+GameWorld）"
git checkout main
git merge --no-ff feature/sp0-foundation -m "sp0: 工程地基完成（_Game/玩家/主菜单/加载/设置/交互桥接/构建）"
```

---

## SP0 完成标准（对照 spec 第 8 节）

- [ ] 主菜单→加载→进世界→移动/交互（商店/箱子菜单可用）→设置可调
- [ ] PC 构建包可正常游玩，零控制台报错
- [ ] PlayMode 测试 4 例全绿；Compile/Build 均通过
- [ ] 全部代码命名空间化；桥接边界（Game.Bridge）清晰；Spark/UIS/Samples 源码零修改
- [ ] `main` 分支历史清晰（12 个 `sp0:` 提交）

