using System;
using Character.Player.Skill.Runtime;

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
        void ApplyTargeting(ActiveSkillRuntime runtime, ref Targeting.TargetingConfig config);
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

    /// <summary>
    /// 冷却阶段修改器：在技能使用后应用
    /// 优先级：第四执行
    /// </summary>
    public interface ICooldownModifier : ISkillModifier
    {
        /// <summary>
        /// 应用冷却修改（直接修改 runtime.EffectiveCooldown）
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        void ApplyCooldown(ActiveSkillRuntime runtime);
    }

    // /// <summary>
    // /// 投射物/弹道阶段修改器：在创建投射物时应用
    // /// 优先级：在伤害之后执行
    // /// </summary>
    // public interface IProjectileModifier : ISkillModifier
    // {
    //     /// <summary>
    //     /// 应用投射物配置修改
    //     /// </summary>
    //     /// <param name="runtime">技能运行时状态</param>
    //     /// <param name="config">投射物配置（ref 引用，可直接修改）</param>
    //     void ApplyProjectile(ActiveSkillRuntime runtime, ref Targeting.ProjectileConfig config);
    // }

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
        void ApplyCrossPhase(ActiveSkillRuntime runtime, ref Targeting.SkillTargetContext ctx);
    }

    #endregion

    #region 兼容旧接口

    /// <summary>
    /// 旧版修改器接口（已废弃，保留用于向后兼容）
    /// 请使用具体的分类接口（IEnergyCostModifier, ITargetingModifier 等）
    /// </summary>
    [Obsolete("请使用具体的分类接口（IEnergyCostModifier, ITargetingModifier, IDamageModifier 等）")]
    public interface ILegacySkillModifier : ISkillModifier
    {
        /// <summary>
        /// 执行优先级，数值越小越先执行
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 应用修改器效果
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        /// <param name="ctx">技能目标上下文（ref 引用，可直接修改）</param>
        void Apply(ActiveSkillRuntime runtime, ref Targeting.SkillTargetContext ctx);
    }

    #endregion
}
