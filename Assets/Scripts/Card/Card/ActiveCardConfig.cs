
using System;
using UnityEngine;

[Serializable]
public class ActiveCardConfig
{
    [Header("能量配置")]
    [Tooltip("能量阈值（需要达到此值才能释放技能）")]
    public int energyThreshold = 100;

    [Tooltip("最大能量值（超过此值不再积累）")]
    public int maxEnergy= 100;

    [Tooltip("击杀一名敌人时获得的能量")]
    public int energyPerKill = 10;

    [Tooltip("释放技能后是否清空能量（true=清零，false=只减少阈值）")]
    public bool consumeAllEnergy = true;



    [Tooltip("若为 true 则此卡需要消耗能量才能使用；否则可按冷却使用")]
    public bool requiresCharge = true;

    //
    public SkillDefinition skill;
}
