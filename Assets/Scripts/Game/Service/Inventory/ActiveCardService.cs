using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Events;
using RogueGame.Items;
using RogueGame.Events;
using Character.Player.Skill.Targeting;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 主动卡核心管理服务
    /// </summary>
    public class ActiveCardService
    {
        private readonly List<ActiveCardState> _activeCards = new();

        public IReadOnlyList<ActiveCardState> ActiveCardStates => _activeCards;

        public event Action<string> OnActiveCardInstanceAdded;
        public event Action<string, int> OnActiveCardEnergyChanged;
        public event Action<string> OnActiveCardEquipChanged;

        /// <summary>
        /// 触发能量变化事件（内部方法，统一事件发布逻辑）
        /// </summary>
        private void NotifyEnergyChanged(string instanceId, int newEnergy)
        {
            OnActiveCardEnergyChanged?.Invoke(instanceId, newEnergy);
            EventBus.Publish(new ActiveCardEnergyChangedEvent
            {
                InstanceId = instanceId,
                NewEnergy = newEnergy
            });
        }

        public IEnumerable<ActiveCardView> ActiveCardViews =>
            _activeCards.Select(st => new ActiveCardView
            {
                CardId = st.CardId,
                InstanceId = st.InstanceId,
                IsEquipped = st.IsEquipped,
                EquippedPlayerId = st.EquippedPlayerId,
                Energy = st.CurrentEnergy,
                Level = st.Level
            });

        /// <summary>
        /// 创建新主动卡实例
        /// </summary>
        public string CreateInstance(string cardId, int initialEnergy)
        {
            var state = new ActiveCardState
            {
                CardId = cardId,
                InstanceId = Guid.NewGuid().ToString(),
                CurrentEnergy = Mathf.Max(0, initialEnergy),
                IsEquipped = false,
                EquippedPlayerId = null,
                CooldownRemaining = 0f
            };

            _activeCards.Add(state);
            OnActiveCardInstanceAdded?.Invoke(state.InstanceId);
            return state.InstanceId;
        }

        public ActiveCardState GetCard(string instanceId)
            => _activeCards.Find(c => c.InstanceId == instanceId);

        public ActiveCardState GetFirstByCardId(string cardId)
            => _activeCards.Find(s => s != null && s.CardId == cardId);

        public void EquipCard(string instanceId, string playerId)
        {
            var st = GetCard(instanceId);
            if (st == null) return;

            st.IsEquipped = !string.IsNullOrEmpty(playerId);
            st.EquippedPlayerId = playerId;
            OnActiveCardEquipChanged?.Invoke(instanceId);
        }

        public void RemoveInstance(string instanceId)
        {
            var st = GetCard(instanceId);
            if (st != null) _activeCards.Remove(st);
        }

        public bool RemoveByCardId(string cardId)
        {
            var st = _activeCards.Find(c => c.CardId == cardId);
            if (st != null)
            {
                _activeCards.Remove(st);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _activeCards.Clear();
        }

        public bool HasCard(string cardId)
            => _activeCards.Exists(c => c.CardId == cardId);

        public int GetCount(string cardId)
            => _activeCards.Count(c => c.CardId == cardId);

        #region 能量管理 API

        /// <summary>
        /// 增加技能能量（自动触发 OnActiveCardEnergyChanged 事件）
        /// </summary>
        public void AddEnergy(string instanceId, int amount)
        {
            var st = GetCard(instanceId);
            if (st == null || amount <= 0) return;

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            int maxEnergy = cardDef?.activeCardConfig?.maxEnergy ?? 999;

            int before = st.CurrentEnergy;
            st.CurrentEnergy = Mathf.Min(maxEnergy, st.CurrentEnergy + amount);

            if (before != st.CurrentEnergy)
            {
                NotifyEnergyChanged(instanceId, st.CurrentEnergy);
            }
        }

        /// <summary>
        /// 消耗技能能量（统一入口，根据配置的消耗模式执行不同逻辑）
        /// </summary>
        /// <param name="instanceId">卡牌实例 ID</param>
        /// <param name="costConfig">能量消耗配置（由修改器修改，仅影响 Threshold 模式的数值）</param>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeSkillEnergy(string instanceId, in EnergyCostConfig costConfig)
        {
            var st = GetCard(instanceId);
            if (st == null) return false;

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            if (cardDef?.activeCardConfig == null) return false;

            int before = st.CurrentEnergy;

            // 根据配置的消耗模式执行不同的消耗逻辑
            switch (cardDef.activeCardConfig.consumptionMode)
            {
                case EnergyConsumptionMode.All:
                    // 全部模式：清空能量（忽略修改器）
                    st.CurrentEnergy = 0;
                    break;

                case EnergyConsumptionMode.Threshold:
                    // 阈值模式：扣除修改后的阈值
                    int baseCost = cardDef.activeCardConfig.energyThreshold;
                    int finalCost = costConfig.CalculateFinalCost(baseCost);
                    st.CurrentEnergy = Mathf.Max(0, st.CurrentEnergy - finalCost);
                    break;
            }

            if (before != st.CurrentEnergy)
            {
                NotifyEnergyChanged(instanceId, st.CurrentEnergy);
            }

            return true;
        }

        /// <summary>
        /// 获取当前能量值
        /// </summary>
        public int GetCurrentEnergy(string instanceId)
        {
            return GetCard(instanceId)?.CurrentEnergy ?? 0;
        }

        /// <summary>
        /// 获取最大能量值
        /// </summary>
        public int GetMaxEnergy(string instanceId)
        {
            var st = GetCard(instanceId);
            if (st == null) return 100;

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            return cardDef?.activeCardConfig?.maxEnergy ?? 100;
        }

        /// <summary>
        /// 将技能能量重置为最大值
        /// </summary>
        public void ResetEnergyToMax(string instanceId)
        {
            var st = GetCard(instanceId);
            if (st == null) return;

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            int maxEnergy = cardDef?.activeCardConfig?.maxEnergy ?? 100;

            int before = st.CurrentEnergy;
            st.CurrentEnergy = maxEnergy;

            if (before != st.CurrentEnergy)
            {
                NotifyEnergyChanged(instanceId, st.CurrentEnergy);
            }
        }

        #endregion
    }
}
