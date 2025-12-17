using System.Collections.Generic;
using UnityEngine;
using Character.Components;
using Character.Core;
using Character.Effects;
using Character;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// 技能执行时的上下文：携带施法者、目标、位置、伤害信息等常用数据
    /// SkillDefinition.Execute 可接收此上下文并执行一系列 Effect（或直接调用帮助方法）
    /// </summary>
    public class SkillContext
    {
        public Transform Owner { get; }
        public TeamType OwnerTeam { get; }
        public Vector2 Position { get; set; }
        public Vector2 AimDirection { get; set; }

        /// <summary>
        /// 可选的目标列表（单体技能可放第一个）
        /// </summary>
        public List<GameObject> Targets { get; } = new();

        /// <summary>
        /// 可选的显式目标（由交互式 TargetingModule 在选择完成后填充）。
        /// 如果 Targets 为空但 ExplicitTarget 非空，执行阶段会把 ExplicitTarget 当作单体目标处理。
        /// </summary>
        public GameObject ExplicitTarget { get; set; }

        public SkillContext(Transform owner)
        {
            Owner = owner;
            OwnerTeam = owner != null ? owner.GetComponent<CharacterBase>()?.Team ?? TeamType.Neutral : TeamType.Neutral;
            Position = owner != null ? (Vector2)owner.position : Vector2.zero;
            AimDirection = Vector2.right;
        }

        // ======= 常用帮助方法 =======

        /// <summary>
        /// 对指定目标应用状态效果（封装 StatusEffectComponent.AddEffect）
        /// </summary>
        public void ApplyStatusEffect(GameObject target, params IStatusEffect[] effects)
        {
            if (target == null || effects == null || effects.Length == 0) return;

            var effectcomp = target.GetComponent<StatusEffectComponent>();
            if (effectcomp != null)
            {
                foreach (var effect in effects)
                {
                    effectcomp.AddEffect(effect);
                }
            }
        }

        /// <summary>
        /// 对指定目标造成伤害（封装 HealthComponent.TakeDamage）
        /// </summary>
        public DamageResult ApplyDamage(GameObject target, DamageInfo info)
        {
            if (target == null) return new DamageResult();

            var health = target.GetComponent<HealthComponent>();
            if (health != null)
            {
                return health.TakeDamage(info);
            }

            return new DamageResult();
        }
    }
}
