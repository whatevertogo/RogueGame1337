/// <summary>
/// 主动卡添加结果
/// </summary>
public class ActiveCardAddResult
{
    /// <summary>是否成功处理</summary>
    public bool Success { get; set; }

    /// <summary>是否添加了新卡</summary>
    public bool Added { get; set; }

    /// <summary>是否升级了现有卡</summary>
    public bool Upgraded { get; set; }

    /// <summary>是否转换为金币</summary>
    public bool ConvertedToCoins { get; set; }

    /// <summary>卡牌ID</summary>
    public string CardId { get; set; }

    /// <summary>实例ID（Added 或 Upgraded 时有值）</summary>
    public string InstanceId { get; set; }

    /// <summary>新等级（Upgraded 时有值）</summary>
    public int NewLevel { get; set; }

    /// <summary>获得金币数量（仅当 ConvertedToCoins 为 true 时有值）</summary>
    public int CoinsAmount { get; set; }
}