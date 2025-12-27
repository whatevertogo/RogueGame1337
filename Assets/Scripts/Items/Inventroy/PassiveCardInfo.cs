using System;

namespace RogueGame.Items
{
    /// <summary>
    /// 被动卡信息
    /// </summary>
    [Serializable]
    public struct PassiveCardInfo
    {
        public string CardId;
        public int Count;
    }
}
