namespace Character.Player.Skill.Core
{
    /// <summary>
    /// 技能中断原因
    /// </summary>
    public enum InterruptReason
    {
        /// <summary>
        /// 未知原因
        /// </summary>
        Unknown,

        /// <summary>
        /// 沉默效果
        /// </summary>
        Silence,

        /// <summary>
        /// 眩晕效果
        /// </summary>
        Stun,

        /// <summary>
        /// 新技能替换
        /// </summary>
        NewSkill,

        /// <summary>
        /// 手动打断
        /// </summary>
        ManualInterrupt
    }
}
