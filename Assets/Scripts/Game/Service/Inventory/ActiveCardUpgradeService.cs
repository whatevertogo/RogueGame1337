using CDTU.Utils;
using Core.Events;
using Character.Player;
using Character.Player.Skill.Evolution;
using Character.Player.Skill.Runtime;
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
            var skillDef = ResolveSkillDefinition(state.CardId);
            if (skillDef == null)
                return false;

            if (_effectPool == null)
            {
                Debug.LogError("[ActiveCardUpgradeService] EvolutionEffectPool 未初始化！");
                return false;
            }

            //  获取已选效果列表：优先从运行时，否则从存档恢复（使用 List 支持正确计数）
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
        /// 获取已选效果列表（用于选项过滤）
        /// 优先从运行时获取（如果已装备），否则从存档恢复
        /// 使用 List 而非 HashSet 以支持正确的重复计数和动态衰减
        /// </summary>
        private List<EvolutionEffectEntry> GetChosenEvolutionsAsList(ActiveCardState state)
        {
            // 1. 尝试从运行时获取（如果已装备）
            var runtime = GetSkillRuntime(state.InstanceId);
            if (runtime != null && runtime.ChosenEvolutions.Count > 0)
            {
                return new List<EvolutionEffectEntry>(runtime.ChosenEvolutions);
            }

            // 2. 否则从存档恢复到临时列表（确保未装备卡牌也能正确过滤）
            if (state.ChosenEffectIds.Count > 0)
            {
                return RestoreChosenEvolutionsFromSaveAsList(state.ChosenEffectIds);
            }

            // 3. 没有已选效果
            return new List<EvolutionEffectEntry>();
        }

        /// <summary>
        /// 从存档恢复已选效果到临时列表（用于选项过滤）
        /// 使用 List 而非 HashSet 以支持正确的重复计数
        /// </summary>
        private List<EvolutionEffectEntry> RestoreChosenEvolutionsFromSaveAsList(IReadOnlyList<string> effectIds)
        {
            var list = new List<EvolutionEffectEntry>();

            if (_effectPool == null || effectIds == null)
                return list;

            foreach (string id in effectIds)
            {
                var effect = _effectPool.GetEffectById(id);
                if (effect != null)
                    list.Add(effect);  // List 允许重复添加同一效果
            }

            return list;
        }

        /// <summary>
        /// 获取技能运行时状态（用于获取已选效果）
        /// 遍历所有本地玩家的技能槽位，查找匹配的 InstanceId
        /// ✨ 懒加载：即使卡牌未装备，也从存档恢复已选效果
        /// </summary>
        private ActiveSkillRuntime GetSkillRuntime(string instanceId)
        {
            var playerManager = GameRoot.Instance?.PlayerManager;
            if (playerManager == null)
                return null;

            // 遍历所有本地玩家
            foreach (var playerState in playerManager.GetAllPlayersData())
            {
                if (playerState?.Controller == null) continue;

                var skillComponent = playerState.Controller.GetComponent<PlayerSkillComponent>();
                if (skillComponent == null) continue;

                // 遍历所有槽位
                for (int i = 0; i < skillComponent.SlotCount; i++)
                {
                    var runtime = skillComponent.GetRuntime(i);
                    if (runtime?.InstanceId == instanceId)
                    {
                        // ✨ 懒加载：如果 runtime 尚未恢复已选效果，从存档恢复
                        if (runtime.ChosenEvolutions.Count == 0)
                        {
                            var cardState = _cardService.GetCardByInstanceId(instanceId);
                            if (cardState != null && cardState.ChosenEffectIds.Count > 0 && _effectPool != null)
                            {
                                runtime.LoadEvolutionFromSave(cardState.ChosenEffectIds, _effectPool);
                                CDLogger.Log($"[ActiveCardUpgradeService] 懒加载已选效果: {instanceId}, 恢复 {runtime.ChosenEvolutions.Count} 个效果");
                            }
                        }
                        return runtime;
                    }
                }
            }

            return null;
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
