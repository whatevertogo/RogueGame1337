
using System.Collections.Generic;
using Card.SkillSystem.TargetingSystem;
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;





[CreateAssetMenu(fileName = "Nearest Target Strategy", menuName = "Card System/Skill System/Targeting System/Strategies/Nearest Target Strategy")]
public class NearestTargetStrategy : TargetAcquireSO
{
    [SerializeField] private float Range;
    [SerializeField] private LayerMask targetMask;

    [Header("过滤设置")]
    [Tooltip("是否排除施法者自身")]
    [SerializeField] private bool excludeSelf = true;

    public override List<CharacterBase> Acquire(SkillTargetContext ctx)
    {
        // 保护性检查：施法者不能为空
        if (ctx.Caster == null)
            return new List<CharacterBase>();

        // 优先使用上下文中的范围配置，否则使用配置值
        float searchRange = ctx.Targeting.Range > 0 ? ctx.Targeting.Range : Range;

        Collider[] hitColliders = Physics.OverlapSphere(ctx.Caster.transform.position, searchRange, targetMask);
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