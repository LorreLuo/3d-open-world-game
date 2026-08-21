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
            Assert.AreEqual(CharacterCreationFlow.HairPresets[2], hair.material.color, "发色未应用到预览材质。");

            flow.ApplyOption("", 0, "Leather");
            var stitched = preview.transform.Find("LeatherArmor(Clone)");
            Assert.IsNotNull(stitched, "皮甲未缝合到预览。");
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
            Assert.AreEqual(CharacterCreationFlow.HairPresets[2], hair.material.color, "玩家发色未应用自定义。");
            var stitched = player.transform.Find("LeatherArmor(Clone)");
            Assert.IsNotNull(stitched, "玩家护甲未缝合。");
        }

        static SkinnedMeshRenderer FindRenderer(Transform root, string name)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++) {
                if (renderers[i].name == name) { return renderers[i]; }
            }
            return null;
        }
    }
}
