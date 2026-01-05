using System;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;

namespace Character.Player.Skill.Modifiers
{
    /// <summary>
    /// 技能修改器基接口：所有修改器必须实现此接口以标识类型
    /// </summary>
    public interface ISkillModifier
    {
        /// <summary>
        /// 修改器唯一标识（用于调试和日志）
        /// </summary>
        string ModifierId { get; }

        /// <summary>
        /// 修改器优先级（0-100，默认 50）
        /// 较高优先级先执行，相同优先级顺序不保证
        /// </summary>
        int Priority => 50;  // 默认中位优先级
    }

    #region 阶段化修改器接口

    /// <summary>
    /// 能量消耗阶段修改器：在能量消耗前应用
    /// 优先级：最先执行
    /// </summary>
    public interface IEnergyCostModifier : ISkillModifier
    {
        /// <summary>
        /// 应用能量消耗修改
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        /// <param name="config">能量消耗配置（ref 引用，可直接修改）</param>
        void ApplyEnergyCost(ActiveSkillRuntime runtime, ref Targeting.EnergyCostConfig config);
    }

    /// <summary>
    /// 目标选择阶段修改器：在目标获取前应用
    /// 优先级：第二执行
    /// </summary>
    public interface ITargetingModifier : ISkillModifier
    {
        /// <summary>
        /// 应用目标获取配置修改
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        /// <param name="config">目标获取配置（ref 引用，可直接修改）</param>
        void ApplyTargeting(ActiveSkillRuntime runtime, ref TargetingConfig config);
    }

    /// <summary>
    /// 伤害计算阶段修改器：在应用效果前应用
    /// 优先级：第三执行
    /// </summary>
    public interface IDamageModifier : ISkillModifier
    {
        /// <summary>
        /// 应用伤害计算修改
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        /// <param name="result">伤害计算结果（ref 引用，可直接修改）</param>
        void ApplyDamage(ActiveSkillRuntime runtime, ref DamageResult result);
    }

    #endregion

    #region 效果生成修改器接口

    /// <summary>
    /// 效果生成修改器：动态生成需要应用到目标身上的效果
    /// 用于技能进化分支添加新效果（如燃烧、眩晕、减速等）
    /// 优先级：在效果应用阶段收集所有效果生成器
    /// </summary>
    public interface IEffectGeneratorModifier : ISkillModifier
    {
        /// <summary>
        /// 生成需要应用的效果定义列表
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        /// <returns>效果定义列表（可以为空，但不应返回 null）</returns>
        System.Collections.Generic.List<StatusEffectDefinitionSO> GenerateEffects(ActiveSkillRuntime runtime);
    }

    #endregion

    #region 跨阶段修改器接口

    /// <summary>
    /// 跨阶段修改器：当修改器需要同时访问多个阶段数据时实现
    /// 例如："如果目标数量 > 3，则能量消耗减半"
    /// </summary>
    public interface ICrossPhaseModifier : ISkillModifier
    {
        /// <summary>
        /// 在所有阶段完成后应用（用于处理跨阶段逻辑）
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        /// <param name="ctx">完整的技能目标上下文（ref 引用，可直接修改）</param>
        void ApplyCrossPhase(ActiveSkillRuntime runtime,  SkillContext ctx);
    }

    #endregion

}
