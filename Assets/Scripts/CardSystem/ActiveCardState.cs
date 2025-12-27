using System;

[Serializable]
public class ActiveCardState
{
    public string cardId;            // 索引到 GameRoot.Instance.CardDataBase
    public string instanceId;        // 可选：若卡牌不是全局唯一
    public int currentCharges;
    public float cooldownRemaining;
    public string equippedPlayerId;  // null 表示在池中
}
