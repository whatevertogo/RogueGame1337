using System.Collections.Generic;
using CDTU.Utils;
using Character.Player.Skill.Evolution;
using Core.Events;
using RogueGame.Events;
using RogueGame.Game.Service.Inventory;
using RogueGame.Items;

namespace RogueGame.Game.Service
{
    /// <summary>
    /// InventoryServiceManager
    /// 纯服务 / 领域协调层（无 MonoBehaviour）
    /// 负责协调背包、卡牌、金币、升级等子系统
    /// </summary>
    public sealed class InventoryServiceManager
    {
        #region 子服务

        public CoinService CoinService { get; }
        public ActiveCardService ActiveCardService { get; }
        public PassiveCardService PassiveCardService { get; }
        public ActiveCardUpgradeService ActiveCardUpgradeService { get; }

        #endregion

        #region 构造与初始化

        public InventoryServiceManager()
        {
            CoinService = new CoinService();
            ActiveCardService = new ActiveCardService();
            PassiveCardService = new PassiveCardService();
            ActiveCardUpgradeService = new ActiveCardUpgradeService(ActiveCardService);
        }

        #endregion

        #region 对外只读访问

        public int Coins => CoinService.Coins;
        public IReadOnlyList<ActiveCardState> ActiveCardStates => ActiveCardService.ActiveCardStates;
        public IEnumerable<ActiveCardView> ActiveCardViews => ActiveCardService.ActiveCardViews;
        public IReadOnlyList<PassiveCardInfo> PassiveCards => PassiveCardService.Cards;

        #endregion

        #region 金币 API

        public void AddCoins(int amount) => CoinService.AddCoins(amount);
        public bool SpendCoins(int amount) => CoinService.SpendCoins(amount);
        public void RemoveCoins(int amount) => CoinService.RemoveCoins(amount);
        public void SetCoins(int coins) => CoinService.SetCoins(coins);

        #endregion

        #region 主动卡 API

        public ActiveCardState GetActiveCard(string instanceId)
            => ActiveCardService.GetCardByInstanceId(instanceId);

        public ActiveCardState GetFirstActiveCard(string cardId)
            => ActiveCardService.GetFirstByCardId(cardId);

        public bool HasActiveCard(string cardId)
            => ActiveCardService.HasCard(cardId);

        public int GetActiveCardCount(string cardId)
            => ActiveCardService.GetCount(cardId);

        public void RemoveActiveCardInstance(string instanceId)
            => ActiveCardService.RemoveInstance(instanceId);

        public bool RemoveActiveCardByCardId(string cardId)
            => ActiveCardService.RemoveByCardId(cardId);

        #endregion

        #region 被动卡 API

        public void AddPassiveCard(
            string cardId,
            int count = 1,
            CardAcquisitionSource source = CardAcquisitionSource.Other)
        {
            PassiveCardService.AddCard(cardId, count, source);
        }

        public void RemovePassiveCard(string cardId, int count = 1)
        {
            PassiveCardService.RemoveCard(cardId, count);
        }

        public int GetPassiveCardCount(string cardId)
        {
            return PassiveCardService.GetCount(cardId);
        }

        #endregion

        #region 升级 API

        public int GetActiveCardLevel(string cardId)
            => ActiveCardUpgradeService.GetLevel(cardId);

        /// <summary>
        /// 请求升级（是否成功发起进化请求）
        /// </summary>
        public bool UpgradeActiveCard(string cardId)
            => ActiveCardUpgradeService.UpgradeCard(cardId);

        /// <summary>
        /// 确认进化选择（UI 层调用）- 效果池系统
        /// </summary>
        public bool ConfirmEvolution(
            string instanceId,
            string cardId,
            int currentLevel,
            int nextLevel,
            EvolutionEffectEntry selectedEffect)
        {
            // ✨ 防御性校验：检查选中效果有效性
            if (selectedEffect == null || string.IsNullOrEmpty(selectedEffect.effectId))
            {
                CDLogger.LogError($"[Inventory] 选中的进化效果无效（null 或 effectId 为空）");
                return false;
            }

            // ✨ 防御性校验：等级连续性
            if (nextLevel != currentLevel + 1)
            {
                CDLogger.LogError($"[Inventory] 进化等级不连续: {currentLevel} → {nextLevel}");
                return false;
            }

            // ✨ 防御性校验：软上限
            int maxLevel = GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 999;
            if (nextLevel > maxLevel)
            {
                CDLogger.LogError($"[Inventory] 进化超过软上限: Lv{nextLevel} > Lv{maxLevel}");
                return false;
            }

            var cardState = ActiveCardService.GetCardByInstanceId(instanceId);
            if (cardState == null)
            {
                CDLogger.LogError($"[Inventory] 找不到卡牌实例: {instanceId}");
                return false;
            }

            // ✨ 防御性校验：确保卡牌状态与传入等级一致
            if (cardState.Level != currentLevel)
            {
                CDLogger.LogWarning($"[Inventory] 卡牌等级不匹配: 状态记录 Lv{cardState.Level}, 传入 Lv{currentLevel}");
                // 以状态为准，继续执行（处理可能的异步场景）
            }

            // 更新持久化的等级
            cardState.Level = nextLevel;

            // 保存已选择的效果 ID（用于存档）
            cardState.AddChosenEffect(selectedEffect.effectId);

            // 发布进化完成事件（Runtime 会订阅此事件并应用修改器）
            EventBus.Publish(new SkillEvolvedEvent(
                cardId,
                instanceId,
                nextLevel,
                selectedEffect
            ));

            CDLogger.Log($"[Inventory] '{cardId}' 进化完成 Lv{nextLevel}, 选择效果: {selectedEffect.effectName}");
            return true;
        }

        #endregion

        #region 通用卡牌 API

        public void ClearAllCards()
        {
            ActiveCardService.Clear();
            PassiveCardService.Clear();
        }

        public void AddCardById(string cardId)
        {
            var db = GameRoot.Instance?.CardDatabase;
            if (db == null) return;

            var def = db.Resolve(cardId);
            if (def == null) return;

            if (def.CardType == CardType.Active)
            {
                AddActiveCardSmart(cardId, def.activeCardConfig.energyPerKill);
            }
            else
            {
                AddPassiveCard(cardId, 1, CardAcquisitionSource.EnemyDrop);
            }
        }

        public void RemoveCardById(string cardId)
        {
            var db = GameRoot.Instance?.CardDatabase;
            if (db == null) return;

            var def = db.Resolve(cardId);
            if (def == null) return;

            if (def.CardType == CardType.Active)
            {
                RemoveActiveCardByCardId(cardId);
            }
            else
            {
                RemovePassiveCard(cardId, 1);
            }
        }

        #endregion

        #region 智能添加主动卡（核心业务）

        public ActiveCardAddResult AddActiveCardSmart(string cardId, int initialCharges)
        {
            var result = new ActiveCardAddResult
            {
                CardId = cardId
            };

            var existing = ActiveCardService.GetFirstByCardId(cardId);
            if (existing != null)
            {
                return HandleDuplicateActiveCard(existing, result);
            }

            string instanceId = ActiveCardService.CreateInstance(cardId, initialCharges);
            if (string.IsNullOrEmpty(instanceId))
                return result;

            result.Success = true;
            result.Added = true;
            result.InstanceId = instanceId;
            result.NewLevel = 1;

            CDLogger.Log($"[Inventory] 添加新主动卡 '{cardId}' (Lv1)");
            return result;
        }

    
        private ActiveCardAddResult HandleDuplicateActiveCard(
            ActiveCardState state,
            ActiveCardAddResult result)
        {
            result.InstanceId = state.InstanceId;
            result.NewLevel = state.Level; // 记录当前等级

            // 尝试升级
            bool canUpgrade = UpgradeActiveCard(state.CardId);
            if (canUpgrade)
            {
                result.Success = true;
                result.Upgraded = true;
                result.NewLevel = state.Level + 1;

                CDLogger.Log($"[Inventory] '{state.CardId}' 升级请求已发起 Lv{state.Level} → Lv{state.Level + 1}");
                return result;
            }

            // 已达最大等级，转换为金币
            CDLogger.Log($"[Inventory] '{state.CardId}' 已达最大等级 Lv{state.Level}，尝试转换为金币");
            return ConvertDuplicateToCoins(result);
        }

        private ActiveCardAddResult ConvertDuplicateToCoins(ActiveCardAddResult result)
        {
            var config = GameRoot.Instance?.ActiveCardDeduplicationConfig;
            if (config == null || !config.enableDeduplication)
                return result;

            AddCoins(config.duplicateToCoins);

            result.Success = true;
            result.ConvertedToCoins = true;
            result.CoinsAmount = config.duplicateToCoins;

            if (config.showDeduplicationLog)
            {
                CDLogger.Log($"[Inventory] 重复卡转换为 {result.CoinsAmount} 金币");
            }

            return result;
        }

        #endregion

        #region 能量相关（统一事件）


        /// <summary>
        /// 为指定实例的技能添加能量，成功则发布事件
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="energy"></param>
        public void AddEnergy(string instanceId, int energy)
        {
            ActiveCardService.AddEnergy(instanceId, energy);
            PublishEnergyChanged(GetActiveCard(instanceId));
        }


        /// <summary>
        /// 消耗指定实例的技能能量，成功则发布事件
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="energy"></param>
        /// <returns></returns>
        public bool ConsumeSkillEnergy(string instanceId, int energy)
        {
            bool success = ActiveCardService.ConsumeSkillEnergy(instanceId, energy);
            if (success)
            {
                PublishEnergyChanged(GetActiveCard(instanceId));
            }
            return success;
        }

        public void ResetAllSkillEnergy(string instanceId)
        {
            var card = GetActiveCard(instanceId);
            if (card == null) return;

            ActiveCardService.ResetAllSkillEnergy(instanceId);
            PublishEnergyChanged(card);
        }

        /// <summary>
        /// 为指定玩家的所有装备主动卡添加击杀能量
        /// </summary>
        /// <param name="playerId"></param>
        public void AddChargesForKill(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;

            var db = GameRoot.Instance?.CardDatabase;
            if (db == null) return;

            foreach (var st in ActiveCardService.ActiveCardStates)
            {
                if (st == null || !st.IsEquipped || st.EquippedPlayerId != playerId)
                    continue;

                var def = db.Resolve(st.CardId);
                if (def?.activeCardConfig == null)
                    continue;

                ActiveCardService.AddEnergy(st.InstanceId, def.activeCardConfig.energyPerKill);
                PublishEnergyChanged(st);
            }
        }


        private void PublishEnergyChanged(ActiveCardState card)
        {
            if (card == null || !card.IsEquipped) return;

            var def = GameRoot.Instance?.CardDatabase?.Resolve(card.CardId);
            EventBus.Publish(new ActiveCardEnergyChangedEvent
            {
                InstanceId = card.InstanceId,
                PlayerId = card.EquippedPlayerId,
                NewEnergy = card.CurrentEnergy,
                MaxEnergy = def?.activeCardConfig?.maxEnergy ?? 100
            });
        }

        #endregion
    }
}
