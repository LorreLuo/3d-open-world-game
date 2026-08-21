using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
    }
}
