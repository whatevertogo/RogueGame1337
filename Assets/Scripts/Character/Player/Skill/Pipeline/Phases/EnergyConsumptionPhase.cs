using Character;
using Character.Player.Skill.Core;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;
using Core.Events;
using RogueGame.Events;
using UnityEngine;

namespace Character.Player.Skill.Pipeline.Phases
{
    /// <summary>
    /// 能量消耗阶段：应用修改器 → 消耗能量 → 发布事件
    /// </summary>
    public sealed class EnergyConsumptionPhase : ISkillPhase
    {
        private readonly InventoryServiceManager _inventory;

        public string PhaseName => "EnergyConsumption";

        public EnergyConsumptionPhase(InventoryServiceManager inventory)
        {
            _inventory = inventory;
        }

        public SkillPhaseResult Execute(SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;

            var rt = ctx.Runtime;

            // 1. 应用修改器
            rt.ApplyEnergyCostModifiers(ref ctx.EnergyCost);

            // 2. 消耗能量
            var config = rt.CachedActiveConfig;
            if (config?.requiresCharge == true)
            {
                // 获取基础能量消耗并计算最终消耗
                var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
                int baseCost = cardDef?.activeCardConfig?.energyThreshold ?? 0;
                int finalCost = ctx.EnergyCost.CalculateFinalCost(baseCost);

                if (!_inventory.ConsumeSkillEnergy(rt.InstanceId, finalCost))
                    return SkillPhaseResult.Fail;

                // 记录实际消耗（用于退还）
                rt.ActualEnergyConsumed = finalCost;
                rt.EnergyConsumed = true;
            }

            // 3. 记录 LastUseTime
            rt.LastUseTime = Time.time;

            // 4. 发布事件
            PublishSkillCastEvent(ctx);

            return SkillPhaseResult.Continue;
        }

        private void PublishSkillCastEvent(SkillContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return;

            var controller = caster.GetComponent<PlayerController>();
            if (controller == null) return;

            var playerState = PlayerManager.Instance?.GetPlayerRuntimeStateByController(controller);
            if (playerState != null)
            {
                EventBus.Publish(new PlayerSkillCastEvent(
                    playerState.PlayerId,
                    ctx.SlotIndex,
                    ctx.Runtime.CardId
                ));
            }
        }
    }
}
