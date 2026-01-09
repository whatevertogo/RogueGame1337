// 连击状态封装
public readonly struct ComboState
{
    public readonly int Count; // 当前连击数
    public readonly ComboTier Tier; // 当前档位
    public readonly float RemainingTime; // 剩余窗口时间
    public readonly float EnergyMult; // 能量倍率
    public readonly float SpeedBonus; // 移速加成
    public readonly float RangeBonus; // 范围加成
}
