namespace Character.Player.Skill.Core
{
    /// <summary>
    /// Phase 执行结果：区分"继续"、"取消"、"失败"
    /// Cancel = Token 被取消 / 逻辑中断（退还能量）
    /// Fail = 资源不足 / 条件不满足（不退还能量）
    /// </summary>
    public enum SkillPhaseResult
    {
        /// <summary>
        /// 继续：执行下一个 Phase
        /// </summary>
        Continue,

        /// <summary>
        /// 取消：逻辑中断（触发能量退还）
        /// 用于场景：Token 被取消、协程被替换等
        /// </summary>
        Cancel,

        /// <summary>
        /// 失败：资源不足 / 条件不满足（不退还能量）
        /// 用于场景：能量不足、无目标等
        /// </summary>
        Fail
    }
}
