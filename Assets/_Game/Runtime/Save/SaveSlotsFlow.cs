using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Save
{
    /// <summary>
    /// 主菜单存档槽流程：3 个固定槽位，支持新建（空槽→创建场景）、继续（有档→游戏世界）、删除。
    /// </summary>
    public class SaveSlotsFlow : MonoBehaviour
    {
        [SerializeField] protected Button[] m_SlotButtons = new Button[3];
        [SerializeField] protected Button[] m_DeleteButtons = new Button[3];
        [SerializeField] protected TextMeshProUGUI[] m_SlotLabels = new TextMeshProUGUI[3];
        [SerializeField] protected SceneEntry m_CharacterCreationSceneEntry;
        [SerializeField] protected SceneEntry m_GameWorldSceneEntry;

        protected List<SaveSlotMetadata> m_Slots = new List<SaveSlotMetadata>();

        protected virtual async void Start()
        {
            try {
                await RefreshSlots();
            } catch (System.Exception e) {
                Debug.LogError($"[SaveSlotsFlow] 初始化存档槽失败: {e.Message}");
            }
        }

        protected virtual async Task RefreshSlots()
        {
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            if (save == null) { Debug.LogError("[SaveSlotsFlow] 存档插件不可用。"); return; }

            m_Slots = await save.GetAllSlots();
            m_Slots.Sort((a, b) => a.creationTimestamp.CompareTo(b.creationTimestamp));
            // 防御：正常流程最多 3 槽；若存在残留（如测试遗留）则只显示前 3 个，避免 UI 越界
            if (m_Slots.Count > 3) { m_Slots = m_Slots.GetRange(0, 3); }

            for (int i = 0; i < 3; i++) {
                bool occupied = i < m_Slots.Count;
                if (m_SlotLabels[i] != null) {
                    m_SlotLabels[i].text = occupied
                        ? $"存档 {i + 1} · {m_Slots[i].GetFormattedLastModified()}"
                        : $"空槽位 {i + 1}";
                }
                if (m_DeleteButtons[i] != null) { m_DeleteButtons[i].gameObject.SetActive(occupied); }
                if (m_SlotButtons[i] != null) { m_SlotButtons[i].interactable = true; }
            }
        }

        public virtual async void OnSlotClicked(int index)
        {
            if (index < 0 || index >= 3) { return; }
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            if (save == null) { return; }

            try {
                if (index < m_Slots.Count) {
                    // 继续：加载该槽并进入游戏世界
                    await save.LoadSlot(m_Slots[index].slotId);
                    if (m_GameWorldSceneEntry != null) { SceneLoader.LoadScene(m_GameWorldSceneEntry); }
                } else {
                    // 新游戏：创建槽并进入角色创建
                    var meta = await save.CreateNewSlot($"存档 {index + 1}");
                    if (meta == null) { Debug.LogError("[SaveSlotsFlow] 创建存档槽失败。"); return; }
                    if (m_CharacterCreationSceneEntry != null) { SceneLoader.LoadScene(m_CharacterCreationSceneEntry); }
                }
            } catch (System.Exception e) {
                Debug.LogError($"[SaveSlotsFlow] 槽位操作失败: {e.Message}");
            }
        }

        public virtual async void OnDeleteSlot(int index)
        {
            if (index < 0 || index >= m_Slots.Count) { return; }
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            if (save == null) { return; }

            try {
                await save.DeleteSlot(m_Slots[index].slotId);
                await RefreshSlots();
            } catch (System.Exception e) {
                Debug.LogError($"[SaveSlotsFlow] 删除存档槽失败: {e.Message}");
            }
        }
    }
}
