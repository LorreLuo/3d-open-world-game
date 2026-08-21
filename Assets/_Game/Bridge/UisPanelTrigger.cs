using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.UI.Panels;
using System;
using UnityEngine;

namespace Game.Bridge
{
    /// <summary>
    /// 按面板名打开/关闭 UIS 面板的 Spark 触发器数据。
    /// </summary>
    public class UisPanelTriggerDataAsset : TriggerDataAsset
    {
        [Tooltip("UIS DisplayPanel 唯一名，例如 \"Main Menu\"。")]
        public string panelName = "";
        [Tooltip("true=切换开关；false=直接打开。")]
        public bool toggle;
    }

    /// <summary>
    /// Spark 触发器类型：按名字操作 UIS 面板（SP2 暂停菜单/UI 按钮将复用）。
    /// </summary>
    public class UisPanelTriggerType : TriggerTypeBase
    {
        public override Type GetExpectedDataType()
        {
            return typeof(UisPanelTriggerDataAsset);
        }

        public override bool CanExecute(TriggerExecutionContext context)
        {
            return base.CanExecute(context) && InventorySystemManager.GetDisplayPanelManager(1) != null;
        }

        public override void Execute(TriggerExecutionContext context)
        {
            var data = context.TriggerEntry != null
                ? context.TriggerEntry.GetTriggerData<UisPanelTriggerDataAsset>()
                : null;
            if (data == null) { data = GetData<UisPanelTriggerDataAsset>(); }
            if (data == null || string.IsNullOrEmpty(data.panelName)) {
                Debug.LogWarning("[Game.Bridge] UisPanelTriggerType: panelName 为空。");
                return;
            }
            var manager = InventorySystemManager.GetDisplayPanelManager(1);
            if (data.toggle) { manager.TogglePanel(data.panelName); }
            else { manager.OpenPanel(data.panelName); }
        }
    }
}
