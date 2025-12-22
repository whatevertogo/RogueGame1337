
using System.Collections.Generic;
using CardSystem.SkillSystem.TargetingSystem;
using Character;
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

        Collider[] hitColliders = Physics.OverlapSphere(ctx.Caster.transform.position, Range, targetMask);
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