using UnityEngine;

namespace Game.Runtime.Character
{
    /// <summary>
    /// 角色创建场景流程：预览旋转、换色/换装、确认写入存档并进入游戏世界、返回主菜单。
    /// </summary>
    public class CharacterCreationFlow : MonoBehaviour
    {
        [Tooltip("预览角色根物体（视觉模型）。")]
        [SerializeField] protected Transform m_PreviewRoot;
        [Tooltip("游戏世界场景条目。")]
        [SerializeField] protected SceneEntry m_GameWorldSceneEntry;
        [Tooltip("主菜单场景条目。")]
        [SerializeField] protected SceneEntry m_MainMenuSceneEntry;
        [Tooltip("预览旋转速度（度/秒）。")]
        [SerializeField] protected float m_RotationSpeed = 30f;

        public static readonly Color[] HairPresets =
        {
            new Color(0.10f, 0.08f, 0.06f),
            new Color(0.40f, 0.30f, 0.25f),
            new Color(0.85f, 0.70f, 0.35f),
            new Color(0.70f, 0.25f, 0.20f),
            new Color(0.25f, 0.35f, 0.60f),
        };

        public static readonly Color[] ShirtPresets =
        {
            new Color(0.90f, 0.90f, 0.90f),
            new Color(0.55f, 0.55f, 0.60f),
            new Color(0.30f, 0.50f, 0.80f),
            new Color(0.75f, 0.30f, 0.30f),
            new Color(0.40f, 0.60f, 0.35f),
        };

        public static readonly Color[] BootsPresets =
        {
            new Color(0.35f, 0.20f, 0.15f),
            new Color(0.12f, 0.10f, 0.09f),
            new Color(0.85f, 0.85f, 0.85f),
        };

        protected CharacterCustomization m_Customization;
        protected Color m_HairColor = new Color(0.4f, 0.3f, 0.25f);
        protected Color m_ShirtColor = new Color(0.9f, 0.9f, 0.9f);
        protected Color m_BootsColor = new Color(0.35f, 0.2f, 0.15f);
        protected string m_CurrentOutfitId = "None";

        protected virtual void Start()
        {
            if (m_PreviewRoot == null) {
                Debug.LogError("[Game.Character] CharacterCreationFlow 缺少预览根物体。");
                return;
            }

            m_Customization = new CharacterCustomization(m_PreviewRoot);
            m_Customization.ApplyColor("Hair", m_HairColor);
            m_Customization.ApplyColor("Shirt", m_ShirtColor);
            m_Customization.ApplyColor("Boots", m_BootsColor);
            m_Customization.ApplyOutfit(m_CurrentOutfitId);
        }

        protected virtual void Update()
        {
            if (m_PreviewRoot != null && m_RotationSpeed != 0f) {
                m_PreviewRoot.Rotate(0f, m_RotationSpeed * Time.deltaTime, 0f);
            }
        }

        /// <summary>
        /// 由 CreationOptionButton 调用：partName=Hair/Shirt/Boots 时按预设索引换色；outfitId 非空时换装。
        /// </summary>
        public virtual void ApplyOption(string partName, int presetIndex, string outfitId)
        {
            if (m_Customization == null) { return; }

            if (string.IsNullOrEmpty(outfitId) == false) {
                m_CurrentOutfitId = outfitId;
                m_Customization.ApplyOutfit(outfitId);
                return;
            }

            switch (partName) {
                case "Hair":
                    if (presetIndex >= 0 && presetIndex < HairPresets.Length) { m_HairColor = HairPresets[presetIndex]; }
                    m_Customization.ApplyColor("Hair", m_HairColor);
                    break;
                case "Shirt":
                    if (presetIndex >= 0 && presetIndex < ShirtPresets.Length) { m_ShirtColor = ShirtPresets[presetIndex]; }
                    m_Customization.ApplyColor("Shirt", m_ShirtColor);
                    break;
                case "Boots":
                    if (presetIndex >= 0 && presetIndex < BootsPresets.Length) { m_BootsColor = BootsPresets[presetIndex]; }
                    m_Customization.ApplyColor("Boots", m_BootsColor);
                    break;
                default:
                    Debug.LogWarning($"[Game.Character] 未知部件: {partName}");
                    break;
            }
        }

        public virtual void OnConfirm()
        {
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            if (save != null) {
                save.SetSaveData(new GameCharacterSaveData {
                    hairColor = m_HairColor,
                    shirtColor = m_ShirtColor,
                    bootsColor = m_BootsColor,
                    outfitId = m_CurrentOutfitId,
                });
            }
            if (m_GameWorldSceneEntry != null) { SceneLoader.LoadScene(m_GameWorldSceneEntry); }
        }

        public virtual void OnBack()
        {
            if (m_MainMenuSceneEntry != null) { SceneLoader.LoadScene(m_MainMenuSceneEntry); }
        }
    }
}
