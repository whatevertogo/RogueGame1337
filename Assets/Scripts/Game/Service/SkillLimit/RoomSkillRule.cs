using UnityEngine;

namespace RogueGame.Game.Service.SkillLimit
{
    /// <summary>
    /// 房间技能规则：定义房间对玩家技能使用的限制类型
    /// </summary>
    public enum RoomSkillRule
    {
        /// <summary>
        /// 无限制（默认）：技能正常使用
        /// </summary>
        None,

        /// <summary>
        /// 禁用所有技能：玩家无法使用任何主动技能
        /// </summary>
        DisableAllSkills,

        /// <summary>
        /// 进入房间时重置充能：进入房间时将技能充能重置为满
        /// </summary>
        ResetOnEnter,

        /// <summary>
        /// 技能无冷却：技能不受冷却限制（测试用）
        /// </summary>
        NoCooldown
    }

    /// <summary>
    /// 房间技能规则提供者接口
    /// Room 或 RoomConfig 实现此接口以声明该房间的技能规则
    /// </summary>
    public interface ISkillRuleProvider
    {
        /// <summary>
        /// 该房间的技能规则
        /// </summary>
        RoomSkillRule SkillRule{ get; }
    }
}
