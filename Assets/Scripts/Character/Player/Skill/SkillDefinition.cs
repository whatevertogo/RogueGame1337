using System.Collections.Generic;
using CardSystem.SkillSystem.TargetingSystem;
using UnityEngine;


[CreateAssetMenu(fileName = "NewSkill", menuName = "Card System/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public string skillId;
    public Sprite icon;


    [Tooltip("冷却时间（秒）")]
    public float cooldown = 1f;
    [Tooltip("若大于0，技能触发后延迟检测目标并在延迟结束时应用效果（秒）")]
    public float detectionDelay = 0f;
    public GameObject vfxPrefab;  // 可选


    [Header("Executor")]
    [Tooltip("为该技能指定选择目标方法")]
    //
    public TargetAcquireSO TargetAcquireSO;

    [Header("过滤器")]
    //

    public TargetFilterGroupSO TargetFilters;

    [Header("效果列表")]
    [Tooltip("使用 StatusEffectDefinitionSO（ScriptableObject）来配置技能要应用的效果。运行时会从 Definition 创建实例。")]
    public List<StatusEffectDefinitionSO> Effects;



}
