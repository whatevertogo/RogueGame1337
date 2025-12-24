using System.Collections.Generic;
using Character;
using UnityEngine;
using CardSystem.SkillSystem.TargetingSystem;

/// <summary>
/// 范围伤害目标获取策略
/// 通过球形范围检测获取所有目标，并可应用过滤器进行筛选
/// </summary>
[CreateAssetMenu(fileName = "Area Damage Target Acquire", menuName = "Card System/Skill System/Targeting System/Strategies/Area Damage Target Acquire")]
public class AreaDamageTargetAcquireSO : TargetAcquireSO
{
    [Header("范围设置")]
    [Tooltip("技能作用范围半径")]
    [SerializeField] private float radius = 5f;
    
    [Tooltip("目标检测层级")]
    [SerializeField] private LayerMask targetLayerMask;
    
    [Header("过滤设置")]
    [Tooltip("目标过滤器组，用于筛选符合条件的目标")]
    [InlineEditor]
    [SerializeField] private TargetFilterGroupSO filterGroup;
    
    /// <summary>
    /// 获取指定范围内的所有目标
    /// </summary>
    /// <param name="ctx">技能目标上下文</param>
    /// <returns>符合条件的目标列表</returns>
    public override List<CharacterBase> Acquire(SkillTargetContext ctx)
    {
        List<CharacterBase> resultTargets = new List<CharacterBase>();
        
        // 确定检测中心点，优先使用AimPoint，否则使用Caster位置
        Vector3 detectionCenter = ctx.AimPoint != Vector3.zero ? ctx.AimPoint : ctx.CasterPosition;
        
        // 使用Physics2D.OverlapCircle检测范围内的所有2D碰撞体
        // 注意：2D游戏必须使用Physics2D，而不是Physics.OverlapSphere
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(detectionCenter, radius, targetLayerMask);

        // 遍历检测到的碰撞体，提取CharacterBase组件
        foreach (var collider in hitColliders)
        {
            if (collider == null) continue;

            CharacterBase character = collider.GetComponent<CharacterBase>();
            if (character == null) continue;

            // 如果设置了过滤器组，应用过滤器
            if (filterGroup != null)
            {
                if (!filterGroup.IsValid(ctx, character))
                    continue;
            }

            resultTargets.Add(character);
        }

        return resultTargets;
    }
    
    /// <summary>
    /// 获取范围半径（用于调试和可视化）
    /// </summary>
    public float Radius => radius;
    
    /// <summary>
    /// 获取目标层级掩码（用于调试和可视化）
    /// </summary>
    public LayerMask TargetLayerMask => targetLayerMask;
}