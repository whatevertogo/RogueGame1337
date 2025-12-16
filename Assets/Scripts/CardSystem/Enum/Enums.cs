namespace CardSystem
{

    public enum CardType
    {
        Passive,   // 自动生效 / 常驻
        Active     // 需要玩家触发
    }


    /// <summary>
    /// 卡牌获取相关事件与枚举
    /// </summary>
    public enum CardAcquisitionSource
    {
        Shop,
        Reward,
        Crafting,
        EnemyDrop
    }
}