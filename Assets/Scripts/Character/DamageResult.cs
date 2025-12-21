public class DamageResult
{
    /// <summary>
    /// 造成的最终伤害值（使用浮点以保留小数精度）
    /// </summary>
    public float FinalDamage { get; set; }

    /// <summary>
    /// 伤害来源的标识（例如角色名称或ID）
    /// </summary>
    public string SourceId { get; set; }

    /// <summary>
    /// 表示无伤害的结果（便捷常量）
    /// </summary>
    public static readonly DamageResult Zero = new DamageResult(0f, null);

    /// <summary>
    /// 构造一个伤害结果
    /// </summary>
    public DamageResult(float damage, string sourceId)
    {
        FinalDamage = damage;
        SourceId = sourceId;
    }

    public override string ToString()
    {
        return $"DamageResult {{ FinalDamage = {FinalDamage}, SourceId = {SourceId} }}";
    }
}