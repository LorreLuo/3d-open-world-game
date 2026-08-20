# Spark × UIS 开放世界生存 RPG Demo — v1 总体设计

- 日期：2026-08-20
- 状态：已获用户逐节批准（A~F 设计节）
- 范围：把 Opsive UIS Demo 场景改造为可发布的开放世界生存 RPG 游戏 demo（v1）

---

## 1. 背景与目标

### 1.1 现状

- `Assets\Samples\Opsive Ultimate Inventory System\1.3.8\Demo\Demo.unity` 是 UIS 1.3.8 自带的完整示例场景：自写 CharacterController 第三人称角色、4 个 NPC（商店老板/向导/铁匠/吉普赛人）、3 个敌人、宝箱、钥匙门、拾取物，以及完整的 UIS 背包/商店/制作/强化/热键/血条 UI。**没有任务、对话、场景切换、角色创建、多存档、设置面板**。
- `Assets\Blink\Spark` 是 Blink 出品的无代码 RPG 框架（~6000 文件）：实体系统、命令/事件总线、触发器、条件规则、可播放对象、任务、战斗、职业/种族/制造/货币/物品、捏脸、交互物、多存档、场景切换+加载界面、屏幕文本、游戏设置、第三人称控制器。
- 两框架在**物品系统与存档系统上功能重叠**，需明确职责边界。

### 1.2 最终产品愿景（用户确认）

- 开放世界生存 RPG：自由探索、采集、建造；无固定主线、弱剧情、强系统。
- 后续将扩展：耕地、盖房子等（用户届时提供组件，再做开发）。
- 核心目的：作为大厂 Unity 开发岗面试作品集，并在开发过程中学习 Unity 工程实践。
- 由此确立的质量优先级：**代码架构质量、可读性、可讲解性、干净工程结构 ≥ 内容堆量**。

### 1.3 v1 范围（首个可发布版本，用户确认）

v1 只做四个系统：

1. **战斗与敌人 AI**（Spark Combat/NPCs 插件）
2. **角色创建与捏脸**（Spark Character + CharacterCustomization 插件）
3. **主菜单/加载界面/设置面板**（Spark Scenes + GameSettings 插件）
4. **多存档系统**（Spark Save 插件 + UIS 序列化桥接）

采集、生存数值、制作扩展、建造等系统**不在 v1 内**，等用户提供组件后另行立项。

### 1.4 非目标（YAGNI）

- 不引入 Addressables / DOTween / UniTask（Spark 与 UIS 均不依赖，保持依赖面最小）。
- 不做移动端适配（PC 为主）。
- 不做本地化系统（UI 直接中文，后续需要时再建本地化表）。
- 不动 `Assets\scripts`（用户早期生存原型），后续整合时另行评估。
- 不使用 Spark 的 Items/Crafting/Currency 物品插件（物品唯一事实源是 UIS）。
- v1 不做任务链/新手引导链（弱剧情）。

---

## 2. 关键决策记录（用户已确认）

| # | 决策 | 结论 |
|---|---|---|
| D1 | 游戏愿景 | 开放世界生存 RPG，弱剧情强系统 |
| D2 | 架构分工 | UIS 管物品，Spark 管世界与游戏流，中间写桥接层 |
| D3 | 目标平台 | PC 为主 |
| D4 | UI 语言 | 中文 |
| D5 | v1 系统范围 | 上述 4 个系统 |
| D6 | 角色控制器 | 换用 Spark 第三人称控制器（Cinemachine） |
| D7 | 文件组织 | 新建 `Assets\_Game` 工程目录，Demo 原目录保留参考不再修改 |

---

## 3. 技术基线

### 3.1 环境

| 项 | 值 |
|---|---|
| Unity | 6000.4.5f1（Unity 6.0 LTS） |
| 渲染管线 | URP 17.4.0 |
| Input System | 1.19.0（新旧输入并存，`activeInputHandler: 2`） |
| Cinemachine | 3.1.4 |
| AI Navigation | 2.0.12 |
| TextMeshPro | 随 UGUI 2.0.0 |
| Opsive | UIS 1.3.8（嵌入式包）+ Opsive Shared 2.1.0；**无 UCC** |
| Git | 已初始化，remote `github.com/LorreLuo/3d-open-world-game`，分支 `main` |
| 构建场景 | 目前只有 `Assets/Scenes/SampleScene.unity`（需替换） |

### 3.2 UIS Demo 关键事实（改造基线）

- 主场景 `Demo.unity`（~18.4 万行 YAML）：根对象 `Game`（挂 `InventorySystemManager`、`SaveSystemManager`、`InventorySystemManagerItemSaver`、`Scheduler`、`ObjectPool`、`ItemObjectSpawner`、`EventSystemManager`、`AudioManager`）、`World`、`NPCs`、`Enemies`、`Pickups`、`Main Camera`、`EventSystem`、`DemoCanvas`、`Inventory System Canvas`、`Player Character`（prefab 实例）。
- 玩家 prefab GUID `c6478310d0819754c8a6cca746913aa9`；敌人 prefab GUID `334fdb8d70b5b944e82d47174707be5d`；数据库 `DemoInventoryDatabase.asset` GUID `c2701a9ea6217ea42a694d8410b2e45e`。
- 玩家 prefab 组件：CharacterController + Rigidbody + Animator + Inventory + Equipper + ItemUser + InventoryInteractor + PlayerCharacter + UnityInputSystem + BillboardFX。
- 交互：UIS `Interactable` 组件（GUID `176215d6f5dc7d6439ae4e5225cbd075`）+ 玩家上的 `InventoryInteractor`。
- Demo 脚本在 `Demo\Scripts`，asmdef `Opsive.UltimateInventorySystem.Demo`（GUID `096d6c1262816c04ca09d7ae1d201d8a`），引用 UIS 核心、Shared、TMP、InputSystem。
- 待替换/待处理的 Demo 脚本：`Character.cs`/`CharacterMover.cs`/`CharacterRotator.cs`/`CharacterCamera.cs`（移动相机）、`MeleeAttack.cs`/`RangeAttack.cs`/`RangeAttackBullet.cs`/`CharacterStats.cs`/`CharacterAnimator.cs`（战斗属性）、`EnemyCharacter.cs`/`CharacterNavMeshMover.cs`/`EnemyRespawnerTrigger.cs`（敌人 AI）、`DamagePopupSpawner.cs`/`TextPopup.cs`（飘字）。
- UIS 存档 API（已验证可行）：`SaveSystemManager.SaveAllSavers()`、`LoadAllSavers()`、`GetCurrentSaveDataInfoInternal()`、`SetCurrentSaveDataInternal(SaveData)`；`SaveData` 为 `[Serializable]`（`List<string>` 键 + `List<Serialization>` 值），可被 `JsonUtility` 序列化。存档器：`InventorySaver`、`CurrencyOwnerSaver`、`InventorySystemManagerItemSaver`。

### 3.3 Spark 关键事实（集成约束）

- **全部类型在全局命名空间**（无 namespace）——自写代码必须命名空间化，防止冲突。
- 依赖包：TextMeshPro、InputSystem、Cinemachine（GUID 已在本工程中可用）。无 Addressables/UniTask/DOTween，异步用 `System.Threading.Tasks`。
- 核心 asmdef `Spark.Core` GUID `00a66b3abbb477b42bd871e52fef35d5`。
- 实体：`SparkEntity`（sealed MonoBehaviour）注册到 `SparkEntityRegistry`；**玩家 = 带 `"Player"` tag 的 SparkEntity GameObject**。
- 命令/事件：`ICommand` + `ICommandHandler<T>`，经 `Spark.Network`（`INetworkProvider`，默认 `LocalNetworkProvider` 自动注册）分发；`ISparkEvent`/`SparkEventBus`（静态 `Subscribe`/`Unsubscribe`/`Publish`）。
- 插件服务定位：`Spark.RegisterPlugin<T>()` / `Spark.GetPlugin<T>()`，各插件 `[RuntimeInitializeOnLoadMethod]` 自注册，无需手动初始化。
- 数据库：`SparkDatabaseEntry`（id/displayName/description/icon）资产；`SparkDatabaseRegistry` 编辑器用 `AssetDatabase.FindAssets`、**构建时用 `Resources.LoadAll`** —— 因此**所有 Spark 数据库资产必须位于 `Resources` 目录下**（现有 `Assets\Resources\Database` 已存在）。
- 触发器：`TriggerTypeBase`（ScriptableObject，`Execute(TriggerExecutionContext)`）+ `TriggerEntry` + 内嵌 `TriggerDataAsset`；执行经 `ITriggersPlugin.ExecuteTrigger` → `ExecuteTriggerCommand` → `Spark.Network`。
- 条件：`RequirementTypeBase` + `RequirementEntry` + `RequirementGroupEntry`（And/Or/Nand/Nor/Xor/Threshold），`IRequirementsPlugin.CheckRequirements`。
- 存档：`SaveDataEntry` 抽象基类（`[Serializable]`，JsonUtility 序列化）；`ISaveDataPlugin`：`RegisterSaveDataType<T>()`、`GetSaveData<T>()`、`SetSaveData<T>()`、`SaveAsync()`/`LoadAsync()`、多存档位（`GetSlotManager()`/`CreateNewSlot`/`LoadSlot`/`DeleteSlot`/`GetAllSlots`）、版本管理（`CreateManualVersion`/`LoadVersion`）。默认 provider `SlotAwareLocalFileSaveProvider`，文件在 `Application.persistentDataPath\SaveSlots\<slotId>\save.json`，原子写入（tmp→move）。注册模式：静态 `XxxSaveDataRegistration` 类在 `[RuntimeInitializeOnLoadMethod(AfterAssembliesLoaded)]` 中调用 `RegisterSaveDataType<XxxSaveData>()`。
- 场景：`SceneEntry`（`sceneFileName` = **Build Settings 中的场景名**）；`IScenesPlugin.LoadScene(SceneEntry)`；`SceneLoader` **强依赖场景内存在 `LoadingScreenManager.Instance`**；事件 `SceneLoadStartedEvent`/`SceneLoadCompletedEvent`。
- 游戏设置：`IGameSettingsPlugin`（视频/音频/键位改绑 `StartInteractiveRebind`），UI：`GameSettingsUI` + `VideoSettingsUI`/`AudioSettingsUI`/`KeybindSettingsUI`/`KeybindRebindOverlayUI`。
- 屏幕文本：`IScreenTextsPlugin.DisplayScreenText(eventName, text, worldPos)`，`ScreenTextsManager`/`ScreenTextEntity`，demo prefab `Core\Plugins\ScreenTexts\Demo\Prefabs\ScreenTextEntity.prefab`。
- 控制器：`Spark.ThirdPersonController` asmdef（Cinemachine 跟随）。
- 交互插件：`InteractableObjectEntity : MonoBehaviour, IInteractable`（探测/提示/交互→触发器），玩家挂 `InteractorEntity`，`InteractablesManager` 单例管理指示器与按键提示。
- 捏脸插件：`CharacterCustomizationEntity`、`CustomizationPresetEntry`、`CharacterCustomizationSaveData`；`CharacterEntry` 扩展点 `IPluginExtension`。
- 角色模块：`CharacterEntry`（playerPrefab、playerAvatar、startingScene、startingCoordinates）、`CharacterEntity`、`PlayerAvatarEntity`、`CharacterSaveData`（selectedCharacterId）。
- 战斗插件：`ICombatPlugin`、`AbilityTypeBase`/`AbilityEntry`、`EffectTypeBase`、`StatEntry`、`ValueStatType`/`ResourceStatType`、`DamageTypeEntry`、`StatusTagEntry`、`TargetableEntity`、`PlayerCombatEntity`、`ObjectCombatEntity`、`StatEntity`、`TargetingManager`、`ExecuteAbilityCommand`；`NPCs` 子插件（`InteractableNPCEntity`、`NPCInteractionPanel`）；`AbilityBar` 子插件。
- 编辑器：`Spark > Spark Editor` 窗口由 `PluginManifest` 资产驱动；`Spark > Tools > Folder Automation`/`Database Mover` 用于数据库资产目录规范化。

---

## 4. 系统职责划分（两框架边界）

| 领域 | 负责方 | 说明 |
|---|---|---|
| 物品/背包/装备/制作/商店数据与 UI | UIS | 保留 Demo 全部菜单与 `DemoInventoryDatabase` |
| 拾取物、宝箱、门、货币 | UIS | 现有 prefab 保留 |
| 角色移动/相机 | Spark 第三人称控制器（Cinemachine） | 替换 Demo 自写移动/相机脚本 |
| 实体/玩家识别/事件总线/命令 | Spark Core | `SparkEntity` + `"Player"` tag + `SparkEventBus` + 命令系统 |
| 角色创建/捏脸 | Spark Character + CharacterCustomization | |
| 战斗/属性/能力/仇恨/目标 | Spark Combat | 替换 `MeleeAttack`/`RangeAttack`/`CharacterStats` |
| 敌人 AI | Spark NPCs + Unity NavMesh | 替换 `EnemyCharacter`/`CharacterNavMeshMover` |
| 世界交互入口（探测/提示/交互） | Spark Interactables | 唯一交互入口，UIS 交互组件停用 |
| 存档（多存档位+版本+UI） | Spark Save | UIS 数据经桥接序列化进 Spark 存档；停用 UIS 自带存档 UI |
| 场景切换/加载界面 | Spark Scenes + LoadingScreenManager | |
| 设置面板（画面/音频/键位改绑） | Spark GameSettings | |
| 屏幕飘字/提示文本 | Spark ScreenTexts | 替代 `DamagePopupSpawner`/`TextPopup` |

## 5. 桥接层设计（Game.Bridge）

独立程序集 `Game.Bridge`（同时引用 Spark 各 asmdef 与 UIS 各 asmdef），是两框架**唯一互相接触的地方**（防腐层）。所有桥接类带 `Game.Bridge` 命名空间。v1 桥接组件：

| 桥接类 | 类型 | 归属子项目 | 职责 |
|---|---|---|---|
| `OpenUisMenuTriggerType` + `OpenUisMenuTriggerDataAsset` | Spark `TriggerTypeBase` 子类 | SP0 | 按面板名打开/关闭 UIS `DisplayPanelManager` 面板 |
| `UisInteractionBridge` | Spark `TriggerTypeBase` 或 UnityEvent 回调 | SP0 | Spark 交互物（商店/铁匠/箱子/制作台）触发后拉起对应 UIS 菜单并绑定目标库存 |
| `UisSaveData` + `UisSaveDataRegistration` | Spark `SaveDataEntry` 子类 | SP2 | 存档：`SaveAllSavers()` → `GetCurrentSaveDataInfoInternal().Data` → `JsonUtility.ToJson` 存字符串；读档：`FromJson` → `SetCurrentSaveDataInternal` → `LoadAllSavers()` |
| `UisStatsCombatBridge` | 独立 MonoBehaviour/服务 | SP3 | UIS 装备属性（Attack/Defense）→ Spark `StatEntity`；Spark 伤害结算 → UIS 角色 HP（`DemoCharacterDamageable`/`Damageable`） |
| `SparkDamageScreenText` | 桥接 | SP3 | Spark 伤害事件 → `IScreenTextsPlugin.DisplayScreenText` |

约束：

1. 桥接层外，Spark 代码不得引用 UIS 类型，UIS 代码不得引用 Spark 类型。
2. 自写代码全部命名空间化（`Game.Runtime.*` / `Game.Bridge.*`），禁止新增全局类型名。
3. 桥接层不修改两框架内部源码（UIS 在 `Packages\` 下不可改；Spark 在 `Assets\Blink\Spark` 下原则上不改，若确需修复 bug 走独立补丁并记录）。

## 6. 场景与游戏流程

### 6.1 场景清单（Build Settings 最终形态）

| 场景 | 位置 | 说明 |
|---|---|---|
| `MainMenu.unity` | `Assets\_Game\Scenes\` | 主菜单（新游戏/继续游戏/设置/退出）+ 角色创建与捏脸面板（独立预览相机） |
| `GameWorld.unity` | `Assets\_Game\Scenes\` | 由 Demo 场景改造迁移而来，保留村庄/NPC/敌人/宝箱全部内容 |

`Assets/Scenes/SampleScene.unity` 从构建列表移除。

### 6.2 游戏流程

```
启动 → MainMenu
        ├─ 新游戏：角色创建+捏脸 → 新建 Spark 存档位 → 加载界面 → GameWorld（出生点）
        ├─ 继续游戏：读取最新存档位 → 加载界面 → GameWorld（存档位置/状态）
        ├─ 设置：GameSettings 面板（视频/音频/键位改绑）
        └─ 退出
GameWorld：
  移动（Spark 控制器）→ 交互（Spark Interactables → UIS 菜单）→ 战斗（Spark Combat）→
  暂停菜单（存档/读档/设置/回主菜单）
```

**过渡流程（SP0 验收时）**：角色创建尚未接入（SP1），"新游戏"直接进入 GameWorld 使用默认角色出生；"继续游戏"暂不可用（SP2 接入）。SP1/SP2 完成后流程收敛为上方最终形态。

### 6.3 Spark 数据库资产

`SceneEntry`（MainMenu/GameWorld）、`CharacterEntry`、`AbilityEntry`、`StatEntry`、`DamageTypeEntry`、`ScreenTextEntry` 等全部放置于 `Assets\_Game\Data\Resources\` 下（构建时 `Resources.LoadAll` 依赖此约束），用 Spark 自带 Folder Automation/Database Mover 工具规范化。

## 7. 目录结构

```
Assets/_Game/
├─ Scenes/               MainMenu.unity、GameWorld.unity
├─ Runtime/              Game.Runtime.asmdef（游戏核心自写脚本，命名空间 Game.Runtime.*）
│  ├─ Combat/            战斗桥接与游戏战斗逻辑
│  ├─ Save/              存档 UI 与流程
│  ├─ Character/         角色创建/出生点逻辑
│  ├─ UI/                自建 UI 脚本（主菜单/暂停菜单等）
│  └─ ...
├─ Bridge/               Game.Bridge.asmdef（同时引用 Spark + UIS，命名空间 Game.Bridge.*）
├─ Data/                 Spark 数据库资产（Resources 约束见 6.3）
├─ Prefabs/              改造后的玩家/敌人/UI prefab
└─ Editor/               Game.Editor.asmdef（编辑器工具）
```

## 8. 子项目拆分与验收

每个子项目独立走 设计→计划→实现→验收 循环；验收未通过不进入下一个。

| 子项目 | 内容 | 验收标准 |
|---|---|---|
| **SP0 工程地基** | `_Game` 目录/asmdef/命名空间；Demo 场景迁移为 `GameWorld.unity`（保持可玩）；玩家换 Spark 第三人称控制器；Spark 交互入口 + `UisInteractionBridge`/`OpenUisMenuTriggerType`（商店/箱子等 UIS 菜单可用）；`MainMenu.unity` + Spark 场景切换 + 加载界面；GameSettings 接入；主菜单/设置/加载 UI 中文化；构建配置 | 主菜单→加载→进世界→移动/交互（商店/箱子菜单可用）→设置可调→**PC 构建包可正常游玩，零控制台报错** |
| **SP1 角色创建与捏脸** | `CharacterEntry` 数据库；角色创建面板 + 捏脸 UI（CharacterCustomization）；选择角色 → 出生点进入世界；角色选择写入存档 | 新游戏建角色→捏脸→出生到世界，重进后角色保持 |
| **SP2 多存档** | Spark 存档位 UI（新建/覆盖/删除）；`UisSaveData` 桥接；暂停菜单存档/读档；继续游戏流程 | 保存→退出→读档后物品/背包/货币/角色完整还原 |
| **SP3 战斗与敌人 AI** | Spark Combat 玩家能力（近战/远程）；敌人 `ObjectCombatEntity`+`TargetableEntity`+仇恨/追击/攻击；敌人重生；UIS 装备属性→Spark 属性桥接；Spark 伤害飘字 | 可流畅战斗、敌人死亡重生、装备影响属性、无报错 |

**依赖关系**：SP0 → SP1 → SP2 → SP3（SP0 是地基；SP1 的角色选择先于 SP2 被存档；SP3 独立可并行但放在最后以收敛范围）。

## 9. 风险与对策

| # | 风险 | 对策 |
|---|---|---|
| R1 | Spark 全部类型在全局命名空间，可能与 UIS/自写代码冲突 | 自写代码全命名空间化；SP0 第一步先跑通编译，再做功能 |
| R2 | Spark 数据库资产构建时必须位于 Resources 下 | 用 Spark Folder Automation/Database Mover 工具；SP0 验证构建产物 |
| R3 | 两套交互系统（UIS `Interactable`+`InventoryInteractor` vs Spark `InteractorEntity`+`InteractableObjectEntity`）冲突 | UIS 交互入口停用，统一走 Spark Interactables + `UisInteractionBridge` 拉起 UIS 菜单 |
| R4 | 两套存档系统并存造成数据不一致 | 停用 UIS 存档 UI 与磁盘读写，存档唯一事实源为 Spark；UIS `SaveSystemManager` 仅作内存序列化器被桥接调用 |
| R5 | Demo 场景迁移后引用断裂（Samples 路径 GUID 变化） | 迁移采用 Unity 内场景另存 + prefab 拷贝进 `_Game`，保持 GUID 自动更新；迁移后立即在编辑器验证场景完整性 |
| R6 | 玩家 prefab 改造（换控制器+加 Spark 组件）破坏 UIS 组件联动 | 基于原 prefab 做变体/拷贝改造，逐步验证：先移动，再交互，再 UI |
| R7 | `Assets/scripts` 早期原型与 `SampleScene` 干扰构建 | 保持不动；SP0 验证 Assembly-CSharp 编译通过；构建列表替换 |
| R8 | Spark 自带 Items 插件与 UIS 物品系统概念混淆 | 明确不使用 Spark Items/Crafting/Currency 插件（D 节非目标），防止团队成员误挂组件 |
| R9 | Unity 6 与两框架版本兼容问题 | SP0 即做 PC 构建冒烟测试，问题前置暴露 |

## 10. 验收总标准（v1 发布门槛）

1. PC 构建包：主菜单 → 新游戏（建角色+捏脸）→ 加载 → 世界 → 战斗/交互 → 暂停存档/读档 → 设置面板 → 退出，全程可玩。
2. 全程零控制台报错/异常日志（警告逐条评审）。
3. 存档往返一致性：读档后背包、货币、角色外观、玩家位置完整还原。
4. 代码满足：命名空间化、桥接层边界清晰、关键模块有注释与 README。
5. Git 提交历史清晰（按子项目分阶段提交）。
