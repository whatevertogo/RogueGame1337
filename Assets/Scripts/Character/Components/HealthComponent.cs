using System;
using UnityEngine;
using Character.Interfaces;
using Character.Components;
using Core.Events;
using RogueGame.Events;
using UI.Combat;

/// <summary>
/// 生命组件 - 处理伤害和治疗的入口
/// </summary>
public class HealthComponent : MonoBehaviour, IDamageable, IHealable
    {
        private CharacterStats stats;
        private StatusEffectComponent statusEffects;

        /// <summary>
        /// 当死亡时，通知造成最后一击的攻击者（可能为 null）
        /// 参数：攻击者 GameObject
        /// </summary>
        public event Action<GameObject> OnDeathWithAttacker;

        // 便捷访问
        public float CurrentHP => stats?.CurrentHP ?? 0;
        public float MaxHP => stats?.MaxHP.Value ?? 0;
        public float HPPercent => stats?.HPPercent ?? 0;
        public bool IsDead => stats?.IsDead ?? true;

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
            statusEffects = GetComponent<StatusEffectComponent>();

            if (stats == null)
            {
                CDTU.Utils.CDLogger.LogError($"[HealthComponent] {gameObject.name} 缺少 CharacterStats 组件！");
            }
            else
            {
                stats.OnDeath += HandleDeath;
            }
        }

        private void OnDestroy()
        {
            if (stats != null)
            {
                stats.OnDeath -= HandleDeath;
            }
        }

        public DamageResult TakeDamage(DamageInfo damageInfo)
        {
            if (stats == null || IsDead)
                return DamageResult.Default;

            // 让状态效果修改受到的伤害
            if (statusEffects != null)
            {
                damageInfo.Amount = statusEffects.ModifyIncomingDamage(damageInfo.Amount);
            }

            // 记录最后伤害来源
            lastDamageSource = damageInfo.Source;

            // 实际应用伤害到CharacterStats
            var result = stats.TakeDamage(damageInfo);
            
            // 触发伤害事件用于UI反馈
            TriggerDamageEvent(damageInfo, result);
            
            return result;
        }

        public DamageResult TakeDamage(int amount)
        {
            return TakeDamage(DamageInfo.Create(amount));
        }

        public void Heal(float amount)
        {
            if (stats == null || IsDead || amount <= 0) return;

            float before = stats.CurrentHP;
            stats.Heal(amount);
            float healed = stats.CurrentHP - before;

            // 触发治疗事件
            if (healed > 0)
            {
                TriggerHealEvent(healed);
            }
        }

        public void Heal(int amount) => Heal((float)amount);

        public void FullHeal()
        {
            if (stats == null || IsDead) return;

            float before = stats.CurrentHP;
            stats.FullHeal();
            float healed = stats.CurrentHP - before;

        }

        private GameObject lastDamageSource;

        /// <summary>
        /// 触发伤害事件
        /// </summary>
        private void TriggerDamageEvent(DamageInfo damageInfo, DamageResult result)
        {
            // 发布伤害事件用于UI反馈
            EventBus.Publish(new DamageDealtEvent
            {
                Position = transform.position,
                Damage = Mathf.RoundToInt(result.FinalDamage),
                IsMiss = Mathf.Approximately(result.FinalDamage, 0f),
                Target = gameObject,
                Source = damageInfo.Source
            });
        }

        /// <summary>
        /// 触发治疗事件
        /// </summary>
        private void TriggerHealEvent(float amount)
        {
            EventBus.Publish(new HealEvent
            {
                Position = transform.position,
                Amount = Mathf.RoundToInt(amount),
                Target = gameObject
            });
        }

        private void HandleDeath()
        {
            // 触发扩展事件
            OnDeathWithAttacker?.Invoke(lastDamageSource);
        }

    }