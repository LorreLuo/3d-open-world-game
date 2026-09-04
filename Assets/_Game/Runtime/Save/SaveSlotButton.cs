using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Save
{
    /// <summary>
    /// 主菜单存档槽按钮：点击后按索引转交给 SaveSlotsFlow（新建/继续 或 删除）。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SaveSlotButton : MonoBehaviour
    {
        [Tooltip("槽位索引 0/1/2。")]
        [SerializeField] protected int m_SlotIndex;
        [Tooltip("true = 删除按钮；false = 新建/继续按钮。")]
        [SerializeField] protected bool m_IsDelete;

        protected void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null) { btn.onClick.AddListener(OnClick); }
        }

        protected void OnClick()
        {
            var flow = FindFirstObjectByType<SaveSlotsFlow>();
            if (flow == null) { return; }
            if (m_IsDelete) { flow.OnDeleteSlot(m_SlotIndex); }
            else { flow.OnSlotClicked(m_SlotIndex); }
        }
    }
}
