using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Character
{
    /// <summary>
    /// 角色创建界面里的选项按钮（换色预设 / 换装），点击后把选项转交给 CharacterCreationFlow。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CreationOptionButton : MonoBehaviour
    {
        [Tooltip("部件名：Hair / Shirt / Boots。换装按钮留空。")]
        [SerializeField] protected string m_PartName = "";
        [Tooltip("换色预设索引。")]
        [SerializeField] protected int m_PresetIndex;
        [Tooltip("换装 ID（None / Leather / Knight）。换色按钮留空。")]
        [SerializeField] protected string m_OutfitId = "";

        protected void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null) { btn.onClick.AddListener(OnClick); }
        }

        protected void OnClick()
        {
#if UNITY_6000_5_OR_NEWER
            var flow = FindFirstObjectByType<CharacterCreationFlow>();
#else
            var flow = FindObjectOfType<CharacterCreationFlow>();
#endif
            if (flow != null) { flow.ApplyOption(m_PartName, m_PresetIndex, m_OutfitId); }
        }
    }
}
