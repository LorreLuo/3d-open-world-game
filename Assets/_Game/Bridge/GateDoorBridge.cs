using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Demo.UI;
using UnityEngine;

namespace Game.Bridge
{
    /// <summary>
    /// 大门交互（Spark 交互入口版）：有钥匙开门，无钥匙提示。逻辑平移自 Demo 的 GateDoor。
    /// </summary>
    public class GateDoorBridge : MonoBehaviour
    {
        [Tooltip("开门所需的钥匙物品定义。")]
        [SerializeField] protected DynamicItemDefinition m_GateKey;
        [Tooltip("大门动画控制器。")]
        [SerializeField] protected Animator m_Animator;
        [Tooltip("提示文本面板。")]
        [SerializeField] protected TextPanel m_TextPanel;
        [Tooltip("没有钥匙时显示的文本。")]
        [SerializeField] protected string m_TextIfNoKey = "需要大门钥匙。";
        [Tooltip("有钥匙时显示的文本。")]
        [SerializeField] protected string m_TextHasKey = "大门已打开。";
        [Tooltip("文本显示时长（秒）。")]
        [SerializeField] protected float m_TextDisplayTime = 5f;

        private static readonly int s_Open = Animator.StringToHash("Open");
        protected bool m_DoorOpened;

        public void Open()
        {
            if (m_DoorOpened) { return; }

            var playerEntity = SparkEntityRegistry.GetPlayerEntity();
            if (playerEntity == null) { return; }
            var inventory = playerEntity.GetComponent<Inventory>();
            if (inventory == null) { return; }

            if (inventory.MainItemCollection.HasItem((1, m_GateKey), false)) {
                m_Animator.SetTrigger(s_Open);
                m_DoorOpened = true;
                if (m_TextPanel != null) { m_TextPanel.DisplayText(m_TextHasKey, m_TextDisplayTime); }
            } else {
                if (m_TextPanel != null) { m_TextPanel.DisplayText(m_TextIfNoKey, m_TextDisplayTime); }
            }
        }
    }
}
