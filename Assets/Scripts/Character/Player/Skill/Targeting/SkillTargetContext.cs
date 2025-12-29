using Character;
using UnityEngine;

namespace Character.Player.Skill.Targeting
{
    /// <summary>
    /// 技能施法上下文：用于技能释放过程中的数据传递
    /// 设计原则：单一入口点，通过嵌套结构组织职责，支持 ref 传递零拷贝
    /// </summary>
    public struct SkillTargetContext
    {
        // ========== 阶段1：施法输入（初始化时设置，不应被修改器修改）==========
        public CharacterBase Caster;
        public Vector3 AimPoint;

        /// <summary>
        /// 瞄准方向（归一化向量）
        /// 对于射线、锥形等方向性技能很重要
        /// </summary>
        public Vector3 AimDirection;

        /// <summary>
        /// 技能槽位索引（用于回溯 ActiveSkillRuntime）
        /// </summary>
        public int SlotIndex;

        // ========== 阶段2：目标获取配置（由 TargetingModifier 修改）==========
        /// <summary>
        /// 目标获取配置（由修改器修改）
        /// </summary>
        public TargetingConfig Targeting;

        // ========== 阶段3：能量消耗配置（由 CostModifier 修改）==========
        /// <summary>
        /// 能量消耗配置（由修改器修改）
        /// </summary>
        public EnergyCostConfig EnergyCost;

        // ========== 阶段4：伤害计算结果（由 DamageModifier 修改）==========
        /// <summary>
        /// 伤害计算结果（由修改器修改）
        /// </summary>
        public DamageResult Damage;


        // ========== 便捷属性（不占用存储空间，仅计算属性）==========
        /// <summary>
        /// 施法者位置
        /// </summary>
        public Vector3 CasterPosition => Caster != null ? Caster.transform.position : Vector3.zero;

        /// <summary>
        /// 施法者阵营
        /// </summary>
        public TeamType CasterTeam => Caster != null ? Caster.Team : TeamType.Neutral;
    }

    /// <summary>
    /// 目标获取配置（独立结构体，用于 TargetingModifier）
    /// </summary>
    public struct TargetingConfig
    {
        /// <summary>
        /// 目标获取范围（由修改器修改）
        /// </summary>
        public float Range;

        /// <summary>
        /// 目标数量上限（由修改器修改）
        /// </summary>
        public int MaxCount;

        /// <summary>
        /// 目标获取半径（用于范围技能）
        /// </summary>
        public float Radius;

        /// <summary>
        /// 初始化默认值
        /// </summary>
        public static TargetingConfig Default => new TargetingConfig
        {
            Range = 10f,
            MaxCount = 1,
            Radius = 2f
        };
    }

    /// <summary>
    /// 能量消耗配置（独立结构体，用于 CostModifier）
    /// </summary>
    public struct EnergyCostConfig
    {
        /// <summary>
        /// 能量消耗倍率（由修改器修改，默认 1.0）
        /// </summary>
        public float Multiplier;

        /// <summary>
        /// 能量消耗固定加值（由修改器修改）
        /// </summary>
        public int Flat;

        /// <summary>
        /// 计算最终能量消耗
        /// </summary>
        public int CalculateFinalCost(int baseCost)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseCost * Multiplier) + Flat);
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        public static EnergyCostConfig Default => new EnergyCostConfig
        {
            Multiplier = 1.0f,
            Flat = 0
        };
    }

    
}
