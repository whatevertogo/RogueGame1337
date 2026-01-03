using CDTU.Utils;
using Core.Events;
using Character.Player.Skill.Evolution;
using RogueGame.Events;
using RogueGame.Items;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 主动卡升级服务（负责发起进化请求）
    /// </summary>
    public class ActiveCardUpgradeService
    {
        private readonly ActiveCardService _cardService;

        private int MaxLevel =>
            GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 5;

        public ActiveCardUpgradeService(ActiveCardService cardService)
        {
            _cardService = cardService;
        }

        public int GetLevel(string cardId)
        {
            return _cardService.GetFirstByCardId(cardId)?.Level ?? 0;
        }

        /// <summary>
        /// 尝试升级主动卡（仅发起进化请求）
        /// </summary>
        public bool UpgradeCard(string cardId)
        {
            var state = _cardService.GetFirstByCardId(cardId);
            if (state == null)
                return false;

            if (!CanUpgrade(state.Level))
            {
                CDLogger.Log($"[ActiveCardUpgradeService] Card already at max level Lv{state.Level}");
                return false;
            }

            return RequestEvolution(state);
        }

        private bool CanUpgrade(int currentLevel)
        {
            return currentLevel < MaxLevel;
        }

        private bool RequestEvolution(ActiveCardState state)
        {
            var skillDef = ResolveSkillDefinition(state.CardId);
            if (skillDef == null)
                return false;

            int nextLevel = state.Level + 1;
            var evolutionNode = skillDef.GetEvolutionNode(nextLevel);

            if (evolutionNode == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] No evolution node for Lv{nextLevel}");
                return false;
            }

            EventBus.Publish(new SkillEvolutionRequestedEvent(
                state.CardId,
                state.InstanceId,
                state.Level,
                nextLevel,
                evolutionNode));

            //TODO-添加进化UI触发？
            return false;


            CDLogger.Log($"[ActiveCardUpgradeService] Evolution requested Lv{state.Level} -> Lv{nextLevel}");
            return true;
        }

        private SkillDefinition ResolveSkillDefinition(string cardId)
        {
            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(cardId);
            if (cardDef == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] CardDefinition not found: {cardId}");
                return null;
            }

            var skillDef = cardDef.activeCardConfig?.skill;
            if (skillDef == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] SkillDefinition not found: {cardId}");
                return null;
            }

            return skillDef;
        }
    }
}
