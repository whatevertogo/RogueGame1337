using System;

[Serializable]
public class ActiveCardState
{
    public string cardId;            // 索引到 GameRoot.Instance.CardDataBase
    public string instanceId;        // 可选：若卡牌不是全局唯一，使用 GUID（你选 A 的话可等于 cardId）
    public int currentCharges;
    public float cooldownRemaining;
    public string equippedPlayerId;  // null 表示在池中
}
