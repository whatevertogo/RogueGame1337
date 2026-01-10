using Character;
using Character.Components;
using Core.Events;
using RogueGame.Events;
using UnityEngine;

namespace CombatFeedback.Combo
{
    /// <summary>
    /// 连击档位加成应用组件。订阅 ComboTierChangedEvent，将档位加成应用到 CharacterStats。
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class ComboBuffComponent : MonoBehaviour
    {
        private CharacterStats _stats;

        // 当前激活的修饰器（用于移除）
        private StatModifier _moveSpeedModifier;
        private StatModifier _attackSpeedModifier;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ComboTierChangedEvent>(OnComboTierChanged);
            EventBus.Subscribe<ComboExpiredEvent>(OnComboExpired);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ComboTierChangedEvent>(OnComboTierChanged);
            EventBus.Unsubscribe<ComboExpiredEvent>(OnComboExpired);
            RemoveAllBuffs();
        }

        private void OnComboTierChanged(ComboTierChangedEvent evt)
        {
            ApplyTierBuffs(evt.ComboTier);
        }

        private void OnComboExpired(ComboExpiredEvent evt)
        {
            RemoveAllBuffs();
        }

        private void ApplyTierBuffs(ComboTier tier)
        {
            // 移除旧修饰器
            RemoveAllBuffs();

            // 应用新修饰器（使用 PercentAdd 类型：百分比加成）
            if (tier.speedBonus != 0f)
            {
                _moveSpeedModifier = new StatModifier(
                    tier.speedBonus,
                    StatModType.PercentAdd,
                    500,    // Order: 高优先级，确保在基础值之后应用
                    this   // Source: 使用 this 作为来源，便于追踪和清理
                );
                _stats.MoveSpeed.AddModifier(_moveSpeedModifier);
            }

            if (tier.attackSpeedBonus != 0f)
            {
                _attackSpeedModifier = new StatModifier(
                    tier.attackSpeedBonus,
                    StatModType.PercentAdd,
                    500,    // Order: 高优先级，确保在基础值之后应用
                    this   // Source: 使用 this 作为来源，便于追踪和清理
                );
                _stats.AttackSpeed.AddModifier(_attackSpeedModifier);
            }
        }

        private void RemoveAllBuffs()
        {
            if (_moveSpeedModifier != null)
            {
                _stats.MoveSpeed.RemoveModifier(_moveSpeedModifier);
                _moveSpeedModifier = null;
            }

            if (_attackSpeedModifier != null)
            {
                _stats.AttackSpeed.RemoveModifier(_attackSpeedModifier);
                _attackSpeedModifier = null;
            }
        }
    }
}
