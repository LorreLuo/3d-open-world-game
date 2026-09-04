using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Game.Runtime.Save;

namespace Game.Tests
{
    public class Sp2SmokeTests
    {
        [UnityTest]
        public IEnumerator Save_Data_Types_Registered()
        {
            LogAssert.ignoreFailingMessages = true;
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            Assert.IsNotNull(save, "存档插件不可用。");

            save.SetSaveData(new GameCharacterSaveData { hairColor = Color.red });
            save.SetSaveData(new GameProgressSaveData { sceneName = "GameWorld", playerPosition = new Vector3(1, 2, 3) });

            var c = save.GetSaveData<GameCharacterSaveData>();
            var p = save.GetSaveData<GameProgressSaveData>();
            Assert.IsNotNull(c, "GameCharacterSaveData 未注册。");
            Assert.IsNotNull(p, "GameProgressSaveData 未注册。");
            Assert.AreEqual(Color.red, c.hairColor);
            Assert.AreEqual("GameWorld", p.sceneName);
            Assert.AreEqual(new Vector3(1, 2, 3), p.playerPosition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Slot_Save_Load_RoundTrip()
        {
            LogAssert.ignoreFailingMessages = true;
            bool done = false;
            string result = "";
            RunRoundTrip(r => { done = true; result = r; });
            yield return new WaitUntil(() => done);
            Assert.AreEqual("OK", result, "存档槽往返失败: " + result);
        }

        static async void RunRoundTrip(System.Action<string> onDone)
        {
            try {
                var save = Spark.GetPlugin<ISaveDataPlugin>();
                var meta = await save.CreateNewSlot("测试存档");
                if (meta == null) { onDone("创建槽失败"); return; }

                save.SetSaveData(new GameCharacterSaveData { hairColor = Color.red, outfitId = "Leather" });
                save.SetSaveData(new GameProgressSaveData { sceneName = "GameWorld", playerPosition = new Vector3(5, 1, -3) });
                bool saved = await save.SaveAndUpdateMetadata();
                if (!saved) { onDone("保存失败"); return; }

                save.ClearAllData();
                await save.LoadSlot(meta.slotId);

                var c = save.GetSaveData<GameCharacterSaveData>();
                var p = save.GetSaveData<GameProgressSaveData>();
                bool ok = c != null && c.hairColor == Color.red && c.outfitId == "Leather"
                       && p != null && p.sceneName == "GameWorld" && p.playerPosition == new Vector3(5, 1, -3);

                await save.DeleteSlot(meta.slotId);
                onDone(ok ? "OK" : "数据不匹配");
            } catch (System.Exception e) {
                onDone(e.Message);
            }
        }

        [UnityTest]
        public IEnumerator MainMenu_Has_SaveSlotsFlow()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return null;

            var flow = Object.FindFirstObjectByType<SaveSlotsFlow>();
            Assert.IsNotNull(flow, "主菜单缺少 SaveSlotsFlow。");
            Assert.IsNotNull(GameObject.Find("Slot0Button"), "缺少 Slot0Button。");
            Assert.IsNotNull(GameObject.Find("Slot2Button"), "缺少 Slot2Button。");
        }

        [UnityTest]
        public IEnumerator GameWorld_Has_AutoSave()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;

            var autoSave = Object.FindFirstObjectByType<GameWorldAutoSave>();
            Assert.IsNotNull(autoSave, "GameWorld 缺少 GameWorldAutoSave。");
        }

        [UnityTest]
        public IEnumerator GameWorld_AutoSave_Writes_Progress()
        {
            LogAssert.ignoreFailingMessages = true;
            var save = Spark.GetPlugin<ISaveDataPlugin>();

            // 先创建活动槽（GameWorldAutoSave 仅在存在活动槽时写入）
            bool slotDone = false;
            string slotId = null;
            RunAsync(async () => {
                var meta = await save.CreateNewSlot("自动存档测试");
                slotId = meta != null ? meta.slotId : null;
                slotDone = true;
            });
            yield return new WaitUntil(() => slotDone);
            Assert.IsNotNull(slotId, "创建槽失败。");

            // 进入 GameWorld 触发自动存档
            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var p = save.GetSaveData<GameProgressSaveData>();
            Assert.IsNotNull(p, "GameWorldAutoSave 未写入进度数据。");
            Assert.AreEqual("GameWorld", p.sceneName, "自动存档 sceneName 错误。");

            // 清理测试槽
            bool delDone = false;
            RunAsync(async () => { await save.DeleteSlot(slotId); delDone = true; });
            yield return new WaitUntil(() => delDone);
        }

        [UnityTest]
        public IEnumerator Player_Restores_Progress_Position()
        {
            LogAssert.ignoreFailingMessages = true;
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            save.SetSaveData(new GameProgressSaveData { sceneName = "GameWorld", playerPosition = new Vector3(3, 0, 3) });
            save.SetSaveData(new GameCharacterSaveData());

            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var players = GameObject.FindGameObjectsWithTag("Player");
            Assert.IsTrue(players.Length > 0, "找不到玩家。");
            var pos = players[0].transform.position;
            Assert.AreEqual(3f, pos.x, 0.5f, $"玩家 X 未恢复到存档位置: {pos}");
            Assert.AreEqual(3f, pos.z, 0.5f, $"玩家 Z 未恢复到存档位置: {pos}");
        }

        static async void RunAsync(System.Func<System.Threading.Tasks.Task> task)
        {
            try { await task(); }
            catch (System.Exception e) { Debug.LogError(e); }
        }
    }
}
