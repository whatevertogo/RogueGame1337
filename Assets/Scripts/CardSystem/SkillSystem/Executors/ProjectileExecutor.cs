using UnityEngine;
using Character.Projectiles;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// 投射物执行器 - 发射投射物
    /// 注意：投射物的伤害由 ProjectileBase 在命中时直接计算并应用，
    /// 而不是通过 StatusEffect 系统。这是两种不同的伤害模式。
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileExecutor", menuName = "Card System/Executors/Projectile")]
    public class ProjectileExecutor : SkillExecutorSO
    {
        [Header("投射物配置")]
        [Tooltip("投射物配置 SO，包含速度、伤害倍率等参数")]
        public ProjectileConfig projectileConfig;
        
        [Header("伤害设置")]
        [Tooltip("基础伤害值")]
        public float baseDamage = 10f;
        
        [Tooltip("是否基于施法者攻击力")]
        public bool useAttackPower = true;
        
        [Tooltip("攻击力倍率")]
        public float attackPowerMultiplier = 1f;

        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            if (projectileConfig == null)
            {
                Debug.LogWarning("[ProjectileExecutor] projectileConfig is null");
                return;
            }

            if (ctx.Caster == null)
            {
                Debug.LogWarning("[ProjectileExecutor] Caster is null");
                return;
            }

            // 计算发射位置和方向
            Vector3 origin = ctx.Caster.transform.position;
            Vector2 direction = Vector2.right; // 默认方向
            
            if (ctx.AimPoint.HasValue)
            {
                direction = (ctx.AimPoint.Value - origin).normalized;
            }

            // 计算最终伤害
            float finalDamage = baseDamage;
            if (useAttackPower)
            {
                var stats = ctx.Caster.GetComponent<Character.Components.CharacterStats>();
                if (stats != null)
                {
                    finalDamage += stats.AttackPower.Value * attackPowerMultiplier;
                }
            }

            // 使用 ProjectileSpawner 发射投射物
            // 注意：ProjectileSpawner.Spawn 需要的参数是 (config, pos, dir, damage, team, owner, hitMask)
            ProjectileSpawner.Spawn(
                projectileConfig,
                origin,
                direction,
                finalDamage,
                ctx.Caster.Team,
                ctx.Caster.transform,
                projectileConfig.hitMask,
                skill.Effects?.ToArray()
            );
        }
    }
}
