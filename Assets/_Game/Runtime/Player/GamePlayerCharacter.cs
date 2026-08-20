using Opsive.Shared.Game;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Demo.CharacterControl;
using Opsive.UltimateInventorySystem.Demo.Damageable;
using Opsive.UltimateInventorySystem.Equipping;
using Opsive.UltimateInventorySystem.ItemActions;
using Opsive.UltimateInventorySystem.UI.Panels.Hotbar;
using UnityEngine;

namespace Game.Runtime.Player
{
    /// <summary>
    /// Spark 控制器时代的玩家角色：替代 UIS Demo 的 Character/PlayerCharacter。
    /// 移除旧移动/旋转/相机/输入逻辑，保留属性、动画、死亡重生、快捷栏与伤害飘字注册。
    /// </summary>
    public class GamePlayerCharacter : MonoBehaviour
    {
        [Tooltip("角色基础属性。")]
        [SerializeField] protected Stats m_BaseStats;
        [Tooltip("死亡后是否重生。")]
        [SerializeField] protected bool m_RespawnOnDeath = true;
        [Tooltip("重生位置（世界坐标）。")]
        [SerializeField] protected Vector3 m_RespawnPosition = new Vector3(0, 1, 0);
        [Tooltip("物品快捷栏。")]
        [SerializeField] protected ItemHotbar m_ItemHotbar;

        protected Animator m_Anim;
        protected Inventory m_Inventory;
        protected IEquipper m_Equipper;
        protected ItemUser m_ItemUser;
        protected GamePlayerDamageable m_Damageable;
        protected CharacterStats m_CharacterStats;
        protected CharacterAnimator m_CharacterAnimator;

        public CharacterStats CharacterStats => m_CharacterStats;
        public CharacterAnimator CharacterAnimator => m_CharacterAnimator;
        public GamePlayerDamageable Damageable => m_Damageable;
        public Inventory Inventory => m_Inventory;
        public ItemUser ItemUser => m_ItemUser;
        public ItemHotbar ItemHotbar => m_ItemHotbar;

        protected virtual void Awake()
        {
            m_Anim = GetComponent<Animator>();
            m_Inventory = GetComponent<Inventory>();
            m_Equipper = GetComponent<IEquipper>();
            m_ItemUser = GetComponent<ItemUser>();
            m_Damageable = GetComponent<GamePlayerDamageable>();
            m_CharacterStats = new CharacterStats(m_BaseStats, m_Equipper);
            m_CharacterAnimator = new CharacterAnimator(m_Anim);
            Physics.IgnoreLayerCollision(8, 10);
        }

        protected virtual void Start()
        {
            DamagePopupSpawner.RegisterDamageable(m_Damageable, DamagePopupSpawner.DamageableType.PLAYER);
            if (m_ItemHotbar == null) {
#if UNITY_6000_5_OR_NEWER
                m_ItemHotbar = FindAnyObjectByType<ItemHotbar>();
#else
                m_ItemHotbar = FindObjectOfType<ItemHotbar>();
#endif
            }
        }

        public virtual void Die()
        {
            gameObject.SetActive(false);
            if (m_RespawnOnDeath) { Scheduler.Schedule(0.5f, Respawn); }
        }

        public virtual void Respawn()
        {
            m_Damageable.Heal(int.MaxValue, false);
            transform.position = m_RespawnPosition;
            gameObject.SetActive(true);
        }

        protected virtual void OnDestroy()
        {
            DamagePopupSpawner.UnregisterDamageable(m_Damageable, DamagePopupSpawner.DamageableType.PLAYER);
        }
    }
}
