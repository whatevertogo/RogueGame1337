namespace Character.Player.Skill.Targeting
{
    /// <summary>
    /// 能量消耗模式（决定释放技能后的消耗行为）
    /// </summary>
    public enum EnergyConsumptionMode
    {
        /// <summary>
        /// 阈值模式：只扣除 energyThreshold（支持修改器 Multiplier 和 Flat）
        /// </summary>
        Threshold,

        /// <summary>
        /// 全部模式：清空所有能量（不支持修改器）
        /// </summary>
        All
    }
}
