using System;
using CDTU.Utils;
using RogueGame.Items;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 主动卡升级服务
    /// </summary>
    public class ActiveCardUpgradeService
    {
        private readonly ActiveCardService _cardService;

        public event Action<string, int> OnCardLevelUp;

        public ActiveCardUpgradeService(ActiveCardService cardService)
        {
            _cardService = cardService;
        }

        public int GetLevel(string cardId)
        {
            var st = _cardService.GetFirstByCardId(cardId);
            return st?.Level ?? 0;
        }

        public int UpgradeCard(string cardId, int? maxLevel = null)
        {
            int actualMaxLevel = maxLevel ?? (GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 5);
            var st = _cardService.GetFirstByCardId(cardId);
            if (st == null) return -1;

            if (st.Level >= actualMaxLevel)
            {
                return st.Level;
            }

            st.Level++;
            OnCardLevelUp?.Invoke(cardId, st.Level);
            CDLogger.Log($"[ActiveCardUpgradeService] '{cardId}' 升级至 Lv{st.Level}/{actualMaxLevel}");
            return st.Level;
        }
    }
}
