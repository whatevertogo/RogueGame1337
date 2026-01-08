using System.Collections.Generic;
using Character;
using UnityEngine;
using Card.SkillSystem.TargetingSystem;
using Character.Player.Skill.Targeting;

/// <summary>
/// 范围伤害目标获取策略
/// 通过球形范围检测获取所有目标，并可应用过滤器进行筛选
/// </summary>
[CreateAssetMenu(fileName = "AreaDamageTargetAcquire", menuName = "RogueGame/Skill/Targeting/Strategies/AreaDamageTargetAcquire")]
public class AreaDamageTargetAcquireSO : TargetAcquireSO
{
    [Tooltip("目标检测层级")]
    [SerializeField] private LayerMask targetLayerMask;
    
    
    /// <summary>
    /// 获取指定范围内的所有目标
    /// </summary>
    /// <param name="ctx">技能目标上下文</param>
    /// <returns>符合条件的目标列表</returns>
    public override List<CharacterBase> Acquire(SkillContext ctx)
    {
        List<CharacterBase> resultTargets = new List<CharacterBase>();
        
        // 确定检测中心点，优先使用AimPoint，否则使用Caster位置
        Vector3 detectionCenter = ctx.AimPoint != Vector3.zero ? ctx.AimPoint : ctx.CasterPosition;
        
        // 使用Physics2D.OverlapCircle检测范围内的所有2D碰撞体
        // 从上下文中获取目标半径
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(detectionCenter, ctx.Targeting.Radius, targetLayerMask);

        // 遍历检测到的碰撞体，提取CharacterBase组件
        foreach (var collider in hitColliders)
        {
            if (collider == null) continue;

            CharacterBase character = collider.GetComponent<CharacterBase>();
            if (character == null) continue;

            resultTargets.Add(character);
        }

        return resultTargets;
    }
    
    /// <summary>
    /// 获取目标层级掩码（用于调试和可视化）
    /// </summary>
    public LayerMask TargetLayerMask => targetLayerMask;
}