namespace RogueGame.Items
{
    /// <summary>
    /// 主动卡视图数据（只读）
    /// </summary>
    public struct ActiveCardView
    {
        public string CardId;
        public string InstanceId;
        public bool IsEquipped;
        public string EquippedPlayerId;
        public int Energy;
        public int Level;
    }
}
