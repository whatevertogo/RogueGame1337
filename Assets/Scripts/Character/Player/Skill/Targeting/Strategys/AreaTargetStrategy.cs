using System.Collections.Generic;
using Card.SkillSystem.TargetingSystem;
using CDTU.Utils;
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;

/// <summary>
/// 根据鼠标瞄准点为中心，获取一定范围内的所有目标
/// </summary>
[CreateAssetMenu(fileName = "Area Target Strategy", menuName = "Card System/Skill System/Targeting System/Strategies/Area Target Strategy")]
public class AreaTargetStrategy : TargetAcquireSO
{
    [SerializeField] private LayerMask targetMask;
    public override List<CharacterBase> Acquire(SkillContext ctx)
    {
        // 保护性检查：优先确保 ctx.Caster 或 AimPoint 合理（ctx 为 struct）
        Vector3 aimPoint = ctx.AimPoint;

        // 使用配置值
        float searchRadius =  ctx.Targeting.Radius;

        // 边界检查：当半径为 0 或负数时，不执行 OverlapSphere，直接返回空列表
        if (searchRadius <= 0f)
        {
            CDLogger.LogWarning("[AreaTargetStrategy] Search radius is non-positive, no targets acquired.");
            // 若有需要，这里也可以增加日志或使用默认半径
            return new List<CharacterBase>();
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(aimPoint, searchRadius, targetMask);
        List<CharacterBase> targets = new List<CharacterBase>();

        foreach (var collider in hitColliders)
        {
            if (collider == null) continue;
            CharacterBase character = collider.GetComponent<CharacterBase>();
            if (character != null)
            {
                targets.Add(character);
            }
        }

        return targets;
    }


}