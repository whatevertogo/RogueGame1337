using System;
using System.Collections.Generic;
using Card.SkillSystem.TargetingSystem;
using Character.Player.Skill.Evolution;
using Character.Player.Skill.Runtime;
using UnityEngine;


[CreateAssetMenu(fileName = "Skill", menuName = "RogueGame/Card/Skill")]
public class SkillDefinition : ScriptableObject
{
    [Header("基础信息")]
    public string skillId;
    public Sprite icon;

    [Header("技能配置")]
    [Tooltip("若大于0，技能触发后延迟检测目标并在延迟结束时应用效果（秒）")]
    public float detectionDelay = 0f;
    public GameObject vfxPrefab; 

    [Header("技能标签（效果池匹配）")]
    [Tooltip("用于与进化效果池的标签匹配（requiredTags/excludedTags）")]
    public List<SkillTag> skillTags = new List<SkillTag>();

    [Header("Executor")]
    [Tooltip("为该技能指定选择目标方法")]
    public TargetAcquireSO TargetAcquireSO;

    [Header("过滤器")]
    public TargetFilterGroupSO TargetFilters;

    [Header("效果列表")]
    [Tooltip("使用 StatusEffectDefinitionSO（ScriptableObject）来配置技能要应用的效果。运行时会从 Definition 创建实例。")]
    public List<StatusEffectDefinitionSO> Effects;

    /// <summary>
    /// 获取所有效果（基础 + 修改器生成）
    /// 重构：委托给 Runtime 的缓存机制，避免重复计算
    /// </summary>
    /// <param name="runtime">技能运行时状态</param>
    /// <returns>合并后的效果列表</returns>
    public List<StatusEffectDefinitionSO> GetAllEffects(ActiveSkillRuntime runtime)
    {
        if (runtime == null)
        {
            // 如果没有 runtime，只返回基础效果
            return Effects ?? new List<StatusEffectDefinitionSO>();
        }

        // 使用 Runtime 的缓存机制
        return runtime.GetAllEffects();
    }

    // ========== 标签匹配方法（效果池系统） ==========

    /// <summary>
    /// 检查技能是否包含任意一个指定标签
    /// 用于进化效果的标签兼容性检查
    /// </summary>
    /// <param name="tags">要检查的标签列表</param>
    /// <returns>如果包含任意一个标签则返回 true</returns>
    public bool HasAnyTag(List<SkillTag> tags)
    {
        if (tags == null || tags.Count == 0)
            return false;

        if (skillTags == null || skillTags.Count == 0)
            return false;

        foreach (var tag in tags)
        {
            if (skillTags.Contains(tag))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检查技能是否包含指定标签
    /// </summary>
    /// <param name="tag">要检查的标签</param>
    /// <returns>如果包含该标签则返回 true</returns>
    public bool HasTag(SkillTag tag)
    {
        return skillTags != null && skillTags.Contains(tag);
    }
}
