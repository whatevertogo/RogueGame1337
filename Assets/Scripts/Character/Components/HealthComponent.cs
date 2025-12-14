using System;
using UnityEngine;
using Character.Core;
using Character.Interfaces;

namespace Character.Components
{
    /// <summary>
    /// 生命组件 - 处理伤害和治疗的入口
    /// </summary>
    public class HealthComponent : MonoBehaviour, IDamageable, IHealable
    {
        private CharacterStats stats;
        private StatusEffectComponent statusEffects;

        public event Action<DamageResult> OnDamaged;
        public event Action<float> OnHealed;
        public event Action OnDeath;
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
                Debug.LogError($"[HealthComponent] {gameObject.name} 缺少 CharacterStats 组件！");
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
                return new DamageResult();

            // 让状态效果修改受到的伤害
            if (statusEffects != null)
            {
                damageInfo.Amount = statusEffects.ModifyIncomingDamage(damageInfo.Amount);
            }

            // 记录最后伤害来源
            lastDamageSource = damageInfo.Source;

            var result = stats.TakeDamage(damageInfo);

            if (!result.IsDodged)
            {
                OnDamaged?.Invoke(result);
            }

            return result;
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(DamageInfo.Create(amount));
        }

        public void Heal(float amount)
        {
            if (stats == null || IsDead || amount <= 0) return;

            float before = stats.CurrentHP;
            stats.Heal(amount);
            float healed = stats.CurrentHP - before;

            if (healed > 0)
            {
                OnHealed?.Invoke(healed);
            }
        }

        public void Heal(int amount) => Heal((float)amount);

        public void FullHeal()
        {
            if (stats == null || IsDead) return;

            float before = stats.CurrentHP;
            stats.FullHeal();
            float healed = stats.CurrentHP - before;

            if (healed > 0)
            {
                OnHealed?.Invoke(healed);
            }
        }

        private GameObject lastDamageSource;

        private void HandleDeath()
        {
            // 触发扩展事件
            OnDeathWithAttacker?.Invoke(lastDamageSource);
            OnDeath?.Invoke();
        }

    }
}