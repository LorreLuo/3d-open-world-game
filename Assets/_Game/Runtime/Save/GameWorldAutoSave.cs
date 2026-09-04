using UnityEngine;

namespace Game.Runtime.Save
{
    /// <summary>
    /// 进入 GameWorld 时自动存档：写入场景 + 玩家位置，并落盘到当前活动槽位。
    /// </summary>
    public class GameWorldAutoSave : MonoBehaviour
    {
        protected virtual async void Start()
        {
            try {
                // 等一帧，确保场景对象（玩家）已就绪
                await System.Threading.Tasks.Task.Yield();

                var save = Spark.GetPlugin<ISaveDataPlugin>();
                if (save == null) { return; }

                // 无活动槽位（例如直接从编辑器进入 GameWorld）则跳过
                var meta = await save.GetCurrentSlotMetadata();
                if (meta == null) {
                    Debug.Log("[GameWorldAutoSave] 无活动存档槽，跳过自动存档。");
                    return;
                }

                var players = GameObject.FindGameObjectsWithTag("Player");
                var player = players.Length > 0 ? players[0] : null;

                save.SetSaveData(new GameProgressSaveData {
                    sceneName = "GameWorld",
                    playerPosition = player != null ? player.transform.position : Vector3.zero,
                });

                bool ok = await save.SaveAndUpdateMetadata();
                Debug.Log($"[GameWorldAutoSave] 自动存档 {(ok ? "成功" : "失败")}（槽位: {meta.slotId}）");
            } catch (System.Exception e) {
                Debug.LogError($"[GameWorldAutoSave] 自动存档异常: {e.Message}");
            }
        }
    }
}
