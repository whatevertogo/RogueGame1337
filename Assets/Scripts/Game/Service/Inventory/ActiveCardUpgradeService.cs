using System;
using CDTU.Utils;
using Core.Events;
using Character.Player.Skill.Evolution;
using RogueGame.Events;
using RogueGame.Items;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 主动卡升级服务
    /// </summary>
    public class ActiveCardUpgradeService
    {
        private readonly ActiveCardService _cardService;

        public ActiveCardUpgradeService(ActiveCardService cardService)
        {
            _cardService = cardService;
        }

        public int GetLevel(string cardId)
        {
            var st = _cardService.GetFirstByCardId(cardId);
            return st?.Level ?? 0;
        }

        /// <summary>
        /// 升级主动卡（发布进化请求事件，UI 层订阅并打开）
        /// </summary>
        /// <param name="cardId">卡牌ID</param>
        /// <param name="maxLevel">可选的最大等级覆盖</param>
        /// <returns>当前等级（升级前）/ -1 表示卡牌不存在</returns>
        public int UpgradeCard(string cardId, int? maxLevel = null)
        {
            var st = _cardService.GetFirstByCardId(cardId);
            if (st == null) return -1;

            int actualMaxLevel = maxLevel ?? (GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 5);
            if (st.Level >= actualMaxLevel)
            {
                CDLogger.Log($"[ActiveCardUpgradeService] '{cardId}' 已达最大等级 Lv{st.Level}");
                return st.Level;
            }

            // 发布进化请求事件，UI 层订阅事件后会自动打开并显示
            bool requested = RequestEvolution(st.InstanceId);

            if (requested)
            {
                CDLogger.Log($"[ActiveCardUpgradeService] '{cardId}' 发起进化请求 Lv{st.Level}→Lv{st.Level + 1}");
            }

            return st.Level; // 返回当前等级，ConfirmEvolution 会更新等级
        }

        /// <summary>
        /// 请求技能进化（检查条件并发布进化请求事件）
        /// </summary>
        /// <param name="instanceId">卡牌实例ID</param>
        /// <returns>是否成功发起进化请求</returns>
        public bool RequestEvolution(string instanceId)
        {
            var state = _cardService.GetCardByInstanceId(instanceId);
            if (state == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] RequestEvolution failed: InstanceId '{instanceId}' not found");
                return false;
            }

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(state.CardId);
            var skillDef = cardDef?.activeCardConfig?.skill;
            if (skillDef == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] RequestEvolution failed: SkillDefinition for '{state.CardId}' not found");
                return false;
            }

            int nextLevel = state.Level + 1;
            int maxLevel = GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 5;

            if (nextLevel > maxLevel)
            {
                CDLogger.Log($"[ActiveCardUpgradeService] RequestEvolution: '{state.CardId}' already at max level {state.Level}");
                return false;
            }

            var evolutionNode = skillDef.GetEvolutionNode(nextLevel);
            if (evolutionNode == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] RequestEvolution failed: No evolution node for level {nextLevel}");
                return false;
            }

            // 发布进化请求事件，触发 UI
            EventBus.Publish(new SkillEvolutionRequestedEvent(
                state.CardId,
                instanceId,
                state.Level,
                nextLevel,
                evolutionNode
            ));

            CDLogger.Log($"[ActiveCardUpgradeService] RequestEvolution: '{state.CardId}' Lv{state.Level}→Lv{nextLevel}");
            return true;
        }

        /// <summary>
        /// 确认技能进化选择
        /// </summary>
        /// <param name="instanceId">卡牌实例ID</param>
        /// <param name="chooseBranchA">true=选择分支A，false=选择分支B</param>
        /// <returns>是否成功进化</returns>
        public bool ConfirmEvolution(string instanceId, bool chooseBranchA)
        {
            var state = _cardService.GetCardByInstanceId(instanceId);
            if (state == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] ConfirmEvolution failed: InstanceId '{instanceId}' not found");
                return false;
            }

            var skillDef = GameRoot.Instance?.CardDatabase?.Resolve(state.CardId).activeCardConfig.skill;
            if (skillDef == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] ConfirmEvolution failed: SkillDefinition for '{state.CardId}' not found");
                return false;
            }

            int nextLevel = state.Level + 1;
            int maxLevel = GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 5;

            if (nextLevel > maxLevel)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] ConfirmEvolution failed: Already at max level {state.Level}");
                return false;
            }

            var evolutionNode = skillDef.GetEvolutionNode(nextLevel);
            if (evolutionNode == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] ConfirmEvolution failed: No evolution node for level {nextLevel}");
                return false;
            }

            var selectedBranch = chooseBranchA ? evolutionNode.branchA : evolutionNode.branchB;
            if (selectedBranch == null)
            {
                CDLogger.LogWarning($"[ActiveCardUpgradeService] ConfirmEvolution failed: Selected branch is null");
                return false;
            }

            // 更新状态
            state.Level = nextLevel;

            // 记录进化历史
            state.EvolutionHistory.AddChoice(nextLevel, chooseBranchA, selectedBranch.branchName);

            // 构建分支路径字符串
            string branchPath = state.EvolutionHistory.GetPathString();

            // 发布进化完成事件
            EventBus.Publish(new SkillEvolvedEvent(
                state.CardId,
                instanceId,
                nextLevel,
                selectedBranch,
                branchPath
            ));

            CDLogger.Log($"[ActiveCardUpgradeService] ConfirmEvolution: '{state.CardId}' 进化至 Lv{nextLevel}, 选择分支{(chooseBranchA ? "A" : "B")} ({selectedBranch.branchName}), 路径: {branchPath}");
            return true;
        }

        /// <summary>
        /// 获取进化节点的两个分支信息
        /// </summary>
        /// <param name="instanceId">卡牌实例ID</param>
        /// <returns>(分支A, 分支B) 元组</returns>
        public (SkillBranch branchA, SkillBranch branchB) GetEvolutionBranches(string instanceId)
        {
            var state = _cardService.GetCardByInstanceId(instanceId);
            if (state == null) return (null, null);

            var skillDef = GameRoot.Instance?.CardDatabase?.Resolve(state.CardId).activeCardConfig.skill;
            if (skillDef == null) return (null, null);

            int nextLevel = state.Level + 1;
            var evolutionNode = skillDef.GetEvolutionNode(nextLevel);
            if (evolutionNode == null) return (null, null);

            return (evolutionNode.branchA, evolutionNode.branchB);
        }
    }
}
