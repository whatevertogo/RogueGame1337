
using System.Collections.Generic;
using Card.SkillSystem.TargetingSystem;
using CDTU.Utils;
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;




/// <summary>
/// 根据施法者位置，获取一定范围内最近的目标
/// </summary>
[CreateAssetMenu(fileName = "Nearest Target Strategy", menuName = "Card System/Skill System/Targeting System/Strategies/Nearest Target Strategy")]
public class NearestTargetStrategy : TargetAcquireSO
{
    [SerializeField] private LayerMask targetMask;

    [Header("过滤设置")]
    [Tooltip("是否排除施法者自身")]
    [SerializeField] private bool excludeSelf = true;

    public override List<CharacterBase> Acquire(SkillContext ctx)
    {
        // 保护性检查：施法者不能为空
        if (ctx.Caster == null)
            return new List<CharacterBase>();

        // 优先使用上下文中的范围配置，否则使用配置值
        float searchRange =  ctx.Targeting.Range;

        // 边界值检查：当范围小于等于 0 时，直接返回空结果，避免 OverlapSphere 无效调用
        if (searchRange <= 0f)
        {
            CDLogger.LogWarning("[NearestTargetStrategy] Search range is non-positive, no targets acquired.");
            return new List<CharacterBase>();
        }
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(ctx.Caster.transform.position, searchRange, targetMask);
        CharacterBase nearestTarget = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (var collider in hitColliders)
        {
            CharacterBase character = collider.GetComponent<CharacterBase>();
            if (character != null)
            {
                // 排除自身（如果配置要求）
                if (excludeSelf && character == ctx.Caster)
                    continue;

                float distanceSqr = (character.transform.position - ctx.Caster.transform.position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestTarget = character;
                }
            }
        }

        return nearestTarget != null ? new List<CharacterBase> { nearestTarget } : new List<CharacterBase>();
    }
}