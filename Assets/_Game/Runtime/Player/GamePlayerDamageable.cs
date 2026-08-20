using Opsive.UltimateInventorySystem.Demo.Damageable;
using Opsive.UltimateInventorySystem.Demo.Events;
using UnityEngine;
using EventHandler = Opsive.Shared.Events.EventHandler;
using Random = UnityEngine.Random;

namespace Game.Runtime.Player
{
    /// <summary>
    /// 玩家可受伤组件：与 Demo 的 DemoCharacterDamageable 等价，但引用 GamePlayerCharacter。
    /// </summary>
    public class GamePlayerDamageable : Damageable, IDamageable
    {
        [Tooltip("关联的玩家角色。")]
        [SerializeField] protected GamePlayerCharacter m_Character;
        [Tooltip("受击闪烁效果。")]
        [SerializeField] protected Flash m_Flash;

        public override int MaxHp => m_Character.CharacterStats.MaxHp;

        private void OnEnable()
        {
            if (m_Flash != null) { m_Flash.Reset(); }
        }

        public override void TakeDamage(int amount)
        {
            amount -= (int)(m_Character.CharacterStats.Defense * Random.Range(0.9f, 1.1f));
            base.TakeDamage(amount);
            m_Character.CharacterAnimator.Damaged();
            if (gameObject.activeInHierarchy == false) { return; }
            if (m_Flash != null) {
                StartCoroutine(m_Flash.CoroutineIE(Mathf.Clamp(m_InvincibilityTime, 0.4f, 1f)));
            }
        }

        public override void Die()
        {
            m_Character.Die();
            m_Character.CharacterAnimator.Die();
            EventHandler.ExecuteEvent(this, DemoEventNames.c_Damageable_OnDie_Damageable, this);
        }

        private void OnDisable()
        {
            if (m_Flash != null) { m_Flash.Reset(); }
        }
    }
}
