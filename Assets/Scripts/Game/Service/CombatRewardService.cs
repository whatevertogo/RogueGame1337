using RogueGame.Map;

public sealed class CombatRewardEnergyService
{
    private readonly CardDataBase db;
    private readonly InventoryManager inv;
    private readonly GameChargeBalanceConfig balance;

    public CombatRewardEnergyService(
        CardDataBase cardDataBase,
        InventoryManager inventoryManager,
        GameChargeBalanceConfig balanceConfig)
    {
        db = cardDataBase;
        inv = inventoryManager;
        balance = balanceConfig;
    }

    public void GrantKillRewardEnergy(string playerId, RoomType roomType)
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
