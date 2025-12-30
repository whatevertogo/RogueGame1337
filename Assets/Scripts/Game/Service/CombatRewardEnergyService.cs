public sealed class CombatRewardEnergyService
{
    public InventoryServiceManager InventoryManager;


    public CombatRewardEnergyService(InventoryServiceManager inventoryManager)
    {
        InventoryManager = inventoryManager;
    }
    /// <summary>
    /// 为指定玩家发放击杀奖励能量
    /// </summary>
    public void GrantKillRewardEnergy(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return;

        var inventoryManager = GameRoot.Instance?.InventoryManager;
        if (inventoryManager == null) return;

        inventoryManager.AddChargesForKill(playerId);
    }
}
