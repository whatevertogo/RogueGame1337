using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;

namespace Character.Player.Skill.Modifiers
{
    /// <summary>
    /// 技能修改器接口：用于修改技能的运行时行为
    /// 设计参考：StatModifier（用于角色属性修改）
    /// </summary>
    public interface ISkillModifier
    {
        /// <summary>
        /// 执行优先级，数值越小越先执行
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 修改器来源对象，用于标识该修改器的来源（例如某个技能、Buff、装备等）
        /// 常用于批量移除某个来源添加的所有修改器
        /// </summary>
        object Source { get; }

        /// <summary>
        /// 应用修改器效果
        /// </summary>
        /// <param name="runtime">技能运行时状态</param>
        /// <param name="ctx">技能目标上下文（ref 引用，可直接修改）</param>
        void Apply(ActiveSkillRuntime runtime, ref SkillTargetContext ctx);
    }
}
