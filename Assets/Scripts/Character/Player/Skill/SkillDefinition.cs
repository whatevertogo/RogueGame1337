using System.Collections.Generic;
using System.Linq;
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
    public GameObject vfxPrefab;  // 可选

    [Header("进化树（阶段3）")]
    [Tooltip("技能进化节点数组，索引0对应Lv2，索引1对应Lv3，索引2对应Lv4，索引3对应Lv5")]
    public List<SkillNode> evolutionTree;

    [Header("Executor")]
    [Tooltip("为该技能指定选择目标方法")]
    public TargetAcquireSO TargetAcquireSO;

    [Header("过滤器")]
    public TargetFilterGroupSO TargetFilters;

    [Header("效果列表")]
    [Tooltip("使用 StatusEffectDefinitionSO（ScriptableObject）来配置技能要应用的效果。运行时会从 Definition 创建实例。")]
    public List<StatusEffectDefinitionSO> Effects;

    /// <summary>
    /// 技能最高等级（1 + 进化树节点数量）
    /// </summary>
    public int MaxLevel => 1 + (evolutionTree?.Count(x => x != null) ?? 0);

    /// <summary>
    /// 获取指定等级的进化节点
    /// </summary>
    /// <param name="level">技能等级（2-5）</param>
    /// <returns>对应等级的进化节点，不存在则返回null</returns>
    public SkillNode GetEvolutionNode(int level)
    {
        if (evolutionTree == null || level < 2 || level > MaxLevel)
            return null;
        return evolutionTree[level - 2];  // level 2 对应索引 0
    }

    /// <summary>
    /// 检查技能是否可以进化到下一等级
    /// </summary>
    /// <param name="currentLevel">当前等级</param>
    public bool CanEvolve(int currentLevel)
    {
        return currentLevel >= 1 && currentLevel < MaxLevel && GetEvolutionNode(currentLevel + 1) != null;
    }

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
}
