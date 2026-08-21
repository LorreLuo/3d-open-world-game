using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Opsive.UltimateInventorySystem.Demo.CharacterControl.Player;
using Opsive.UltimateInventorySystem.Demo.Damageable;

namespace Game.Tests
{
    public class Sp0SmokeTests
    {
        [UnityTest]
        public IEnumerator GameWorld_Has_Player_With_Spark_Stack()
        {
            // -nographics 无渲染环境会产生 RenderTexture 等噪音日志，本测试只断言结构，忽略日志失败
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;

            var player = SparkEntityRegistry.GetPlayerEntity();
            Assert.IsNotNull(player, "GameWorld 中找不到带 Player 标签的玩家实体。");
            Assert.IsNotNull(player.GetComponent<SparkThirdPersonController>(), "玩家缺少 SparkThirdPersonController。");
            Assert.IsNotNull(player.GetComponent<RulesEntity>(), "玩家缺少 RulesEntity。");
            Assert.IsNotNull(player.GetComponent<InteractorEntity>(), "玩家缺少 InteractorEntity。");
            Assert.IsNotNull(player.GetComponent<PlayerCharacter>(), "玩家缺少 PlayerCharacter。");
            Assert.IsNotNull(player.GetComponent<DemoCharacterDamageable>(), "玩家缺少 DemoCharacterDamageable。");
            Assert.IsNotNull(InteractablesManager.Instance, "场景缺少 InteractablesManager。");
        }

        [UnityTest]
        public IEnumerator Movement_Rules_Exist_With_Defaults()
        {
            LogAssert.ignoreFailingMessages = true;

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
    }
}
