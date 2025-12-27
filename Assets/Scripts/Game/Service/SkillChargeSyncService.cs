using System;
using Character.Player;

public sealed class SkillChargeSyncService
{
    private readonly InventoryServiceManager inv;
    private readonly PlayerManager playerManager;
    private readonly CardDataBase db;

    public SkillChargeSyncService(
        InventoryServiceManager inv,
        PlayerManager playerManager,
        CardDataBase db)
    {
        this.inv = inv;
        this.playerManager = playerManager;
        this.db = db;

        inv.OnActiveCardChargesChanged += OnChargesChanged;
        // 注意：金币变化由 UI 系统处理，此服务不需要关心
    }

    public void OnChargesChanged(string instanceId, int charges)
    {
        foreach (var pr in playerManager.GetAllPlayersData())
        {
            var comp = pr.Controller.GetComponent<PlayerSkillComponent>();
            if (comp == null) continue;

            for (int i = 0; i < comp.PlayerSkillSlots.Length; i++)
            {
                var rt = comp.PlayerSkillSlots[i]?.Runtime;
                if (rt?.InstanceId != instanceId) continue;

                var def = db.Resolve(rt.CardId);
                int max = def?.activeCardConfig.maxEnergy ?? 100;
                float norm = (float)charges / max;

                playerManager.RaisePlayerSkillEnergyChanged(
                    pr.PlayerId, i, norm
                );
            }
        }
    }
}
