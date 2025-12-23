using RogueGame.Map;

public sealed class CombatRewardService
{
    private readonly CardDataBase db;
    private readonly InventoryManager inv;
    private readonly GameBalanceConfig balance;

    public CombatRewardService(
        CardDataBase cardDataBase,
        InventoryManager inventoryManager,
        GameBalanceConfig balanceConfig)
    {
        db = cardDataBase;
        inv = inventoryManager;
        balance = balanceConfig;
    }

    public void GrantKillReward(string playerId, RoomType roomType)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        if (inv == null || db == null || balance == null) return;

        int chargeAmount = balance.GetChargeForRoomType(roomType);

        foreach (var st in inv.ActiveCardStates)
        {
            if (!st.IsEquipped || st.EquippedPlayerId != playerId)
                continue;

            var def = db.Resolve(st.CardId);
            if (def?.activeCardConfig == null) continue;

            inv.AddCharges(
                st.InstanceId,
                chargeAmount,
                def.activeCardConfig.maxCharges
            );
        }
    }
}
