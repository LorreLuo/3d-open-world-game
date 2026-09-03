using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Game.Runtime.Character;

namespace Game.Tests
{
    public class Sp1SmokeTests
    {
        [UnityTest]
        public IEnumerator CharacterCreation_Scene_Loads_With_Flow()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("CharacterCreation");
            yield return null;
            yield return null;

            var flow = Object.FindObjectOfType<CharacterCreationFlow>();
            Assert.IsNotNull(flow, "角色创建场景缺少 CharacterCreationFlow。");
            Assert.IsNotNull(GameObject.Find("CharacterPreview"), "缺少预览角色。");
            Assert.IsNotNull(GameObject.Find("ConfirmButton"), "缺少确认按钮。");
            Assert.IsNotNull(LoadingScreenManager.Instance, "角色创建场景缺少 LoadingScreenManager。");
        }

        [UnityTest]
        public IEnumerator CharacterCreation_SceneEntry_Exists()
        {
            LogAssert.ignoreFailingMessages = true;

            Assert.IsTrue(SparkDatabaseRegistry.HasEntry("scene.CharacterCreation"), "缺少 scene.CharacterCreation 条目。");
            var entry = SparkDatabaseRegistry.GetEntry<SceneEntry>("scene.CharacterCreation");
            Assert.AreEqual("CharacterCreation", entry.sceneFileName, "CharacterCreation 条目场景名错误。");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_Has_CustomizationApplier()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;

            var player = SparkEntityRegistry.GetPlayerEntity();
            Assert.IsNotNull(player, "找不到玩家实体。");
            Assert.IsNotNull(player.GetComponent<PlayerCustomizationApplier>(), "玩家缺少 PlayerCustomizationApplier。");
        }

        [UnityTest]
        public IEnumerator Creation_Applies_Color_And_Outfit_To_Preview()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("CharacterCreation");
            yield return null;
            yield return null;

            var flow = Object.FindObjectOfType<CharacterCreationFlow>();
            Assert.IsNotNull(flow, "缺少 CharacterCreationFlow。");

            flow.ApplyOption("Hair", 2, "");
            var preview = GameObject.Find("CharacterPreview");
            Assert.IsNotNull(preview, "缺少预览角色。");
            var hair = FindRenderer(preview.transform, "Hair");
            Assert.IsNotNull(hair, "预览缺 Hair 渲染器。");
            Assert.AreEqual(CharacterCreationFlow.HairPresets[2], GetEffectiveColor(hair.material), "发色未应用到预览材质。");

            flow.ApplyOption("", 0, "Leather");
            var stitched = preview.transform.Find("LeatherArmor(Clone)");
            Assert.IsNotNull(stitched, "皮甲未缝合到预览。");
        }

        [UnityTest]
        public IEnumerator Creation_ColorButton_Click_Changes_Preview()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("CharacterCreation");
            yield return null;
            yield return null;

            // 模拟真实点击路径：点第 3 个发色色块（HairSwatch2 = 金色）
            var swatch = GameObject.Find("HairSwatch2");
            Assert.IsNotNull(swatch, "缺少 HairSwatch2 色块。");
            var btn = swatch.GetComponent<Button>();
            Assert.IsNotNull(btn, "色块缺 Button 组件。");
            btn.onClick.Invoke();
            yield return null;

            var preview = GameObject.Find("CharacterPreview");
            Assert.IsNotNull(preview, "缺少预览角色。");
            var hair = FindRenderer(preview.transform, "Hair");
            Assert.IsNotNull(hair, "预览缺 Hair 渲染器。");
            Assert.AreEqual(CharacterCreationFlow.HairPresets[2], GetEffectiveColor(hair.material), "点击色块后发色未变化。");
        }

        [UnityTest]
        public IEnumerator Creation_Confirm_Writes_SaveData()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("CharacterCreation");
            yield return null;
            yield return null;

            var flow = Object.FindObjectOfType<CharacterCreationFlow>();
            Assert.IsNotNull(flow, "缺少 CharacterCreationFlow。");

            flow.ApplyOption("Hair", 0, "");
            flow.ApplyOption("", 0, "Knight");
            flow.WriteSaveData();

            var data = Spark.GetPlugin<ISaveDataPlugin>().GetSaveData<GameCharacterSaveData>();
            Assert.IsNotNull(data, "存档数据未写入。");
            Assert.AreEqual(CharacterCreationFlow.HairPresets[0], data.hairColor, "发色未写入存档。");
            Assert.AreEqual("Knight", data.outfitId, "护甲未写入存档。");
        }

        [UnityTest]
        public IEnumerator Player_Applies_Customization_On_Spawn()
        {
            LogAssert.ignoreFailingMessages = true;

            // 模拟创建场景确认：写入自定义数据
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            save.SetSaveData(new GameCharacterSaveData {
                hairColor = CharacterCreationFlow.HairPresets[2],
                shirtColor = CharacterCreationFlow.ShirtPresets[3],
                bootsColor = CharacterCreationFlow.BootsPresets[0],
                outfitId = "Leather",
            });

            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;

            var player = SparkEntityRegistry.GetPlayerEntity();
            Assert.IsNotNull(player, "找不到玩家实体。");
            var hair = FindRenderer(player.transform, "Hair");
            Assert.IsNotNull(hair, "玩家缺 Hair 渲染器。");
            Assert.AreEqual(CharacterCreationFlow.HairPresets[2], GetEffectiveColor(hair.material), "玩家发色未应用自定义。");
            var stitched = player.transform.Find("LeatherArmor(Clone)");
            Assert.IsNotNull(stitched, "玩家护甲未缝合。");
        }

        [UnityTest]
        public IEnumerator Player_Can_Move_Diagnostic()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var player = SparkEntityRegistry.GetPlayerEntity();
            Assert.IsNotNull(player, "找不到玩家实体。");
            var controller = player.GetComponent<SparkThirdPersonController>();
            Assert.IsNotNull(controller, "玩家缺少 SparkThirdPersonController。");

            var rules = Spark.GetPlugin<IRulesPlugin>();
            bool movement = rules.GetRuleValue(player.gameObject, "MOVEMENT");
            bool grounded = controller.isGrounded;
            Vector3 startPos = player.transform.position;
            var diag = $"MOVEMENT={movement} isGrounded={grounded} timeScale={Time.timeScale} startPos={startPos}";

            controller.SetMovementInput(new Vector2(0f, 1f));
            for (int i = 0; i < 30; i++) { yield return null; }
            Vector3 endPos = player.transform.position;
            float dist = Vector3.Distance(startPos, endPos);
            controller.SetMovementInput(Vector2.zero);

            Assert.IsTrue(movement, "MOVEMENT 规则为 false。");
            Assert.IsTrue(dist > 0.01f, $"玩家未移动（dist={dist:F4}）。诊断: {diag} endPos={endPos}");
        }

        [UnityTest]
        public IEnumerator Player_Input_Diagnostic()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var player = SparkEntityRegistry.GetPlayerEntity();
            var rootPi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            Assert.IsNotNull(rootPi, "根节点缺 PlayerInput。");

            var moveAction = rootPi.actions.FindActionMap("Player")?.FindAction("Move");
            var moveEnabled = moveAction != null && moveAction.enabled;
            var diag = $"rootPlayerInput: actions={rootPi.actions?.name} map={rootPi.currentActionMap?.name} enabled={rootPi.enabled} notif={rootPi.notificationBehavior} moveEnabled={moveEnabled} controlScheme={rootPi.defaultControlScheme} devices={rootPi.devices.Count} moveControls={moveAction?.controls.Count} cursorLocked={Cursor.lockState} cursorVisible={Cursor.visible}";

            var childPiGo = player.transform.Find("PlayerInput");
            if (childPiGo != null) {
                var childPi = childPiGo.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                var childUi = childPiGo.GetComponent<Opsive.Shared.Input.InputSystem.UnityInputSystem>();
                diag += $" | child: playerInput={(childPi != null ? childPi.enabled.ToString() : "null")} unityInputSystem={(childUi != null ? childUi.enabled.ToString() : "null")}";
            }

            Debug.Log("[DIAG] " + diag);

            // 断言根 PlayerInput 的 Move 动作已启用
            Assert.IsNotNull(moveAction, "根 PlayerInput 缺少 Player/Move 动作。");
            Assert.IsTrue(moveEnabled, "根 PlayerInput 的 Move 动作未启用。诊断: " + diag);
            // 关键：必须指定控制方案，否则 devices 为空 → 动作无 controls → 键盘输入失效。
            Assert.AreEqual("KeyboardMouse", rootPi.defaultControlScheme, "根 PlayerInput 未指定 KeyboardMouse 控制方案。诊断: " + diag);
        }

        [UnityTest]
        public IEnumerator SparkInputFeeder_Wiring_And_Movement()
        {
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var player = SparkEntityRegistry.GetPlayerEntity();
            Assert.IsNotNull(player, "找不到玩家实体。");
            var controller = player.GetComponent<SparkThirdPersonController>();
            Assert.IsNotNull(controller, "玩家缺少 SparkThirdPersonController。");
            var feeder = player.GetComponent<Game.Runtime.Player.SparkInputFeeder>();
            Assert.IsNotNull(feeder, "玩家缺少 SparkInputFeeder。");
            Assert.IsTrue(feeder.IsWired, "SparkInputFeeder 未绑定 Move 动作（inputActions 或 Player 映射缺失）。");
            Assert.IsTrue(controller.isGrounded, $"玩家未接地（isGrounded={controller.isGrounded}），地面层掩码可能仍有问题。");

            // 控制器能接收并执行移动输入。
            // 注：键盘事件链（QueueStateEvent）在 -nographics 批处理下输入系统不更新设备状态（wKey.isPressed 恒 false），
            // 故这里直接验证控制器移动输入入口；真实键盘驱动由 SparkInputFeeder 在编辑器运行时订阅动作回调完成。
            var startPos = player.transform.position;
            controller.SetMovementInput(new Vector2(0f, 1f));
            for (int i = 0; i < 30; i++) { yield return null; }
            float dist = Vector3.Distance(startPos, player.transform.position);
            controller.SetMovementInput(Vector2.zero);
            Assert.IsTrue(dist > 0.01f, $"玩家未移动（dist={dist:F4}）。isGrounded={controller.isGrounded}");
        }

        static SkinnedMeshRenderer FindRenderer(Transform root, string name)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++) {
                if (renderers[i].name == name) { return renderers[i]; }
            }
            return null;
        }

        static Color GetEffectiveColor(Material mat)
        {
            if (mat.HasProperty("_BaseColorRGBOutlineWidthA")) {
                var v = mat.GetVector("_BaseColorRGBOutlineWidthA");
                return new Color(v.x, v.y, v.z);
            }
            return mat.color;
        }
    }
}
