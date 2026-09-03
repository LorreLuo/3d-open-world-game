using UnityEngine;
using Opsive.UltimateInventorySystem.UI.Panels;

namespace Game.Runtime
{
    /// <summary>
    /// 进入 GameWorld 时恢复时间流动并关闭 Demo 的欢迎面板。
    /// 欢迎面板是菜单面板（m_IsMenuPanel=1）且自动打开（m_OpenOnStart=1），会把 timeScale 置 0 导致人物无法移动。
    /// </summary>
    public class GameWorldBootstrap : MonoBehaviour
    {
        protected void Start()
        {
            Time.timeScale = 1f;

            var welcomeGo = GameObject.Find("Welcome Panel");
            if (welcomeGo != null) {
                var panel = welcomeGo.GetComponent<DisplayPanel>();
                if (panel != null) { panel.SmartClose(); }
            }
        }
    }
}
