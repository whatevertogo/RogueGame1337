namespace Character.Player.Skill.Core
{
    /// <summary>
    /// 技能执行令牌：支持带原因的中断
    /// Phase 只关心 IsCancelled，外部关心 Reason
    /// </summary>
    public sealed class SkillExecutionToken
    {
        /// <summary>
        /// 是否已被取消
        /// </summary>
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// 中断原因
        /// </summary>
        public InterruptReason Reason { get; private set; }

        /// <summary>
        /// 取消执行
        /// </summary>
        /// <param name="reason">中断原因</param>
        public void Cancel(InterruptReason reason = InterruptReason.Unknown)
        {
            IsCancelled = true;
            Reason = reason;
        }
    }
}
