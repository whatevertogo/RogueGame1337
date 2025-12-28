using System.Collections.Generic;
using Character;
using UnityEngine;
using Card.SkillSystem.TargetingSystem;
using Character.Player.Skill.Targeting;

/// <summary>
/// 自我目标获取策略
/// 返回施法者自身作为技能目标，可应用过滤器进行筛选
/// </summary>
[CreateAssetMenu(fileName = "Self Target Acquire", menuName = "Card System/Skill System/Targeting System/Strategies/Self Target Acquire")]
public class SelfTargetAcquireSO : TargetAcquireSO
{
    [Header("过滤设置")]
    [Tooltip("目标过滤器组，用于筛选施法者是否符合条件")]
    [SerializeField] private TargetFilterGroupSO filterGroup;
    
    /// <summary>
    /// 获取施法者自身作为目标
    /// </summary>
    /// <param name="ctx">技能目标上下文</param>
    /// <returns>包含施法者的目标列表（如果通过过滤）</returns>
    public override List<CharacterBase> Acquire(SkillTargetContext ctx)
    {
        List<CharacterBase> resultTargets = new List<CharacterBase>();
        
        // 保护性检查：施法者不能为空
        if (ctx.Caster == null)
            return resultTargets;
        
        // 如果设置了过滤器组，应用过滤器
        if (filterGroup != null)
        {
            if (!filterGroup.IsValid(ctx, ctx.Caster))
                return resultTargets;
        }
        
        resultTargets.Add(ctx.Caster);
        return resultTargets;
    }
}