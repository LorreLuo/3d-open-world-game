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
