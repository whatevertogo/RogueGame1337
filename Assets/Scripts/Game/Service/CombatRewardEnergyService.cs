using RogueGame.Game.Service;

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

        // 使用门面方法：为玩家装备的所有卡增加击杀奖励能量
        // 每张卡根据自己的配置获得不同的能量值
        inventoryManager.AddChargesForKill(playerId);
    }
}
