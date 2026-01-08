namespace Character
{
    /// <summary>
    /// 伤害类型枚举：用于区分不同的伤害计算/抗性逻辑
    /// 可根据项目需要扩展（例如 Elemental、Poison 等）
    /// </summary>
    public enum DamageType
    {
        Physical = 0,
        Magical = 1,
        True = 2
    }
}
