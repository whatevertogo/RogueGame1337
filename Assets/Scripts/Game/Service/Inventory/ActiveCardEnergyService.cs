using UnityEngine;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 主动卡击杀奖励分发服务
    /// </summary>
    public class ActiveCardEnergyService
    {
        private readonly ActiveCardService _cardService;

        public ActiveCardEnergyService(ActiveCardService cardService)
        {
            _cardService = cardService;
        }

        /// <summary>
        /// 为玩家装备的主动卡发放击杀能量
        /// </summary>
        public void AddChargesForKill(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            var db = GameRoot.Instance?.CardDatabase;
            if (db == null) return;

            foreach (var st in _cardService.ActiveCardStates)
            {
                if (st == null || !st.IsEquipped || st.EquippedPlayerId != playerId) continue;
                var def = db.Resolve(st.CardId);
                if (def?.activeCardConfig == null) continue;

                // 委托给 ActiveCardService 处理能量增加
                _cardService.AddEnergy(st.InstanceId, def.activeCardConfig.energyPerKill);
            }
        }
    }
}
