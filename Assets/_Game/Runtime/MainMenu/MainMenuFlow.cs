using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.MainMenu
{
    /// <summary>
    /// 主菜单流程控制。新游戏先进入角色创建场景；继续游戏在 SP2 接入。
    /// </summary>
    public class MainMenuFlow : MonoBehaviour
    {
        [Tooltip("角色创建场景条目（新游戏先进入创建）。")]
        [SerializeField] protected SceneEntry m_CharacterCreationSceneEntry;
        [Tooltip("继续游戏按钮（SP2 前禁用）。")]
        [SerializeField] protected Button m_ContinueButton;

        protected void Start()
        {
            if (m_ContinueButton != null) { m_ContinueButton.interactable = false; }
        }

        public void OnNewGame()
        {
            SceneLoader.LoadScene(m_CharacterCreationSceneEntry);
        }

        public void OnContinue()
        {
            Debug.Log("[Game] 继续游戏将在 SP2 存档系统接入后开放。");
        }

        public void OnSettings()
        {
            Spark.Network.ExecuteCommand(new OpenGameSettingsCommand(0));
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
