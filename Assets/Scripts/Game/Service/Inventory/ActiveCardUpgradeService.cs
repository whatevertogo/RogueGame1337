using CDTU.Utils;
using Core.Events;
using Character.Player.Skill.Evolution;
using RogueGame.Events;
using RogueGame.Items;
using System.Collections.Generic;
using UnityEngine;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 主动卡升级服务（负责发起进化请求）
    /// 使用效果池系统生成进化选项
    /// </summary>
    public class ActiveCardUpgradeService
    {
        private readonly ActiveCardService _cardService;

        /// <summary>
        /// 进化效果池（运行时引用）
        /// </summary>
        private EvolutionEffectPool _effectPool;

        private int MaxLevel =>
            GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 999;

        public ActiveCardUpgradeService(ActiveCardService cardService)
        {
            _cardService = cardService;
        }

        /// <summary>
        /// 初始化升级服务（获取效果池引用）
        /// 在 GameRoot.Awake() 中调用
        /// </summary>
        public void Initialize()
        {
            _effectPool = GameRoot.Instance?.EvolutionEffectPool;
            if (_effectPool == null)
            {
                Debug.LogWarning("[ActiveCardUpgradeService] EvolutionEffectPool 未配置！");
            }
        }

        public int GetLevel(string cardId)
        {
            return _cardService.GetFirstByCardId(cardId)?.Level ?? 0;
        }

        /// <summary>
        /// 尝试升级主动卡（发起进化请求，从效果池获取选项）
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

        /// <summary>
        /// 发起进化请求（效果池系统）
        /// 从全局效果池获取可用选项并发布事件
        /// </summary>
        private bool RequestEvolution(ActiveCardState state)
        {
            var skillDef = GameRoot.Instance.CardDatabase.Resolve(state.CardId).activeCardConfig.skill;
            if (skillDef == null)
                return false;

            if (_effectPool == null)
            {
                Debug.LogError("[ActiveCardUpgradeService] EvolutionEffectPool 未初始化！");
                return false;
            }

            // 获取已选效果列表（从现有Effect存档读取，用于选项过滤的动态权重衰减）
            var chosenEvolutions = GetChosenEvolutionsAsList(state);

            // 从效果池获取选项
            int nextLevel = state.Level + 1;
            var options = _effectPool.GetOptions(skillDef, nextLevel, chosenEvolutions);

            if (options == null || options.Count == 0)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] 没有可用的进化效果: {state.CardId} Lv{nextLevel}");
                return false;
            }

            // 发布进化请求事件（携带选项列表）
            EventBus.Publish(new SkillEvolutionRequestedEvent(
                state.CardId,
                state.InstanceId,
                state.Level,
                nextLevel,
                options));

            CDLogger.Log($"[ActiveCardUpgradeService] 进化请求已发送: {state.CardId} Lv{state.Level} -> Lv{nextLevel}, 选项数量: {options.Count}");
            return true;
        }

        /// <summary>
        /// 获取已选效果列表（用于选项过滤的动态权重衰减）
        /// 直接从存档的 effectId 列表读取，避免遍历运行时
        /// </summary>
        private List<EvolutionEffectEntry> GetChosenEvolutionsAsList(ActiveCardState state)
        {
            if (state?.ChosenEffectIds == null || state.ChosenEffectIds.Count == 0)
                return new List<EvolutionEffectEntry>();

            if (_effectPool == null)
                return new List<EvolutionEffectEntry>();

            var result = new List<EvolutionEffectEntry>();
            foreach (string effectId in state.ChosenEffectIds)
            {
                var effect = _effectPool.GetEffectById(effectId);
                if (effect != null)
                {
                    result.Add(effect);
                }
            }

            return result;
        }
    }
}
