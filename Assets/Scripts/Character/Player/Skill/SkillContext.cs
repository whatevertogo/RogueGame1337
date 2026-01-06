using Character;
using Character.Player.Skill.Runtime;
using Game;
using RogueGame.Game.Service;
using UnityEngine;

namespace Character.Player.Skill.Targeting
{
    /// <summary>
    /// 技能施法上下文：用于技能释放过程中的数据传递
    /// 设计原则：单一入口点，通过嵌套结构组织职责，支持 ref 传递零拷贝
    ///
    /// Context 是「执行快照」，Runtime 是「技能实例」（非所有权）
    /// </summary>
    public class SkillContext
    {
        // ========== 阶段1：施法输入（初始化后不应修改）==========
        /// <summary>
        /// 施法者（只读引用）
        /// </summary>
        public readonly CharacterBase Caster;

        /// <summary>
        /// 瞄准点（只读）
        /// </summary>
        public readonly Vector3 AimPoint;

        /// <summary>
        /// 瞄准方向（归一化向量，只读）
        /// 对于射线、锥形等方向性技能很重要
        /// </summary>
        public readonly Vector3 AimDirection;

        /// <summary>
        /// 技能槽位索引（只读）
        /// </summary>
        public readonly int SlotIndex;

        // ========== 阶段1.5：运行时依赖（只读引用，非所有权）==========
        /// <summary>
        /// 技能运行时状态（只读引用，由 Phase 使用）
        /// </summary>
        public readonly ActiveSkillRuntime Runtime;

        /// <summary>
        /// 库存服务管理器（只读引用，由 Phase 使用）
        /// </summary>
        public readonly InventoryServiceManager Inventory;

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

        // ========== 阶段4：伤害计算结果（由 IDamageModifier 修改）==========
        /// <summary>
        /// 伤害计算结果（由修改器修改）
        /// </summary>
        public DamageResult DamageResult;

        // ========== 阶段6：目标获取结果（策略执行后填充，修改器只读）==========
        /// <summary>
        /// 目标获取结果（由策略填充，供跨阶段修改器读取）
        /// 用于跨阶段修改器（例如："根据目标数量调整能量消耗"）
        /// </summary>
        public TargetResult TargetResult;


        // ========== 便捷属性（不占用存储空间，仅计算属性）==========
        /// <summary>
        /// 施法者位置
        /// </summary>
        public Vector3 CasterPosition => Caster != null ? Caster.transform.position : Vector3.zero;

        /// <summary>
        /// 施法者阵营
        /// </summary>
        public TeamType CasterTeam => Caster != null ? Caster.Team : TeamType.Neutral;


        public SkillContext(CharacterBase caster,
        Vector3 aimPoint,
        Vector3 aimDirection,
         int slotIndex,
        ActiveSkillRuntime runtime,
        InventoryServiceManager inventory,
         TargetingConfig targeting,
        EnergyCostConfig energyCost,
        DamageResult damageResult)
        {
            Caster = caster;
            AimPoint = aimPoint;
            AimDirection = aimDirection;
            SlotIndex = slotIndex;
            Runtime = runtime;
            Inventory = inventory;

            Targeting = targeting;
            EnergyCost = energyCost;
            DamageResult = damageResult;

            TargetResult = TargetResult.Default; // 初始化为默认值，避免未赋值的结构体字段在目标计算与伤害结算流程中被误用
        }


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
        /// 计算最终能量消耗（仅用于 Threshold 模式）
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
