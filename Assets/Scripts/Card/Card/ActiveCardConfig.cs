using System;
using Character.Player.Skill.Targeting;
using UnityEngine;

[Serializable]
public class ActiveCardConfig
{
    [Header("能量配置")]
    [Tooltip("能量阈值（需要达到此值才能释放技能）")]
    public int energyThreshold = 100;

    [Tooltip("最大能量值（超过此值不再积累）")]
    public int maxEnergy = 100;

    [Tooltip("击杀一名敌人时获得的能量")]
    public int energyPerKill = 10;

    [Header("能量消耗模式")]
    [Tooltip("能量消耗模式（决定释放技能后的消耗行为）")]
    public EnergyConsumptionMode consumptionMode = EnergyConsumptionMode.All;

    [Tooltip("若为 true 则此卡需要消耗能量才能使用；否则可按冷却使用")]
    public bool requiresCharge = true;

    //技能定义
    public SkillDefinition skill;

    #region 数据迁移
    [SerializeField, HideInInspector]
    private bool consumeAllEnergy = true;

    /// <summary>
    /// 数据迁移：将旧的 consumeAllEnergy 布尔值映射到新的 consumptionMode 枚举
    /// </summary>
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (consumeAllEnergy)
            consumptionMode = EnergyConsumptionMode.All;
        else
            consumptionMode = EnergyConsumptionMode.Threshold;
    }
#endif
    #endregion
}
