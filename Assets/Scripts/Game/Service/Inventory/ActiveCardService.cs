using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Events;
using RogueGame.Items;
using RogueGame.Events;

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
        public event Action<string, int> OnActiveCardChargesChanged;
        public event Action<string> OnActiveCardEquipChanged;

        /// <summary>
        /// 触发能量变化事件（内部方法，统一事件发布逻辑）
        /// </summary>
        private void NotifyChargesChanged(string instanceId, int newCharges)
        {
            OnActiveCardChargesChanged?.Invoke(instanceId, newCharges);
            EventBus.Publish(new ActiveCardChargesChangedEvent
            {
                InstanceId = instanceId,
                NewCharges = newCharges
            });
        }

        public IEnumerable<ActiveCardView> ActiveCardViews =>
            _activeCards.Select(st => new ActiveCardView
            {
                CardId = st.CardId,
                InstanceId = st.InstanceId,
                IsEquipped = st.IsEquipped,
                EquippedPlayerId = st.EquippedPlayerId,
                Charges = st.CurrentCharges,
                Level = st.Level
            });

        /// <summary>
        /// 创建新主动卡实例
        /// </summary>
        public string CreateInstance(string cardId, int initialCharges)
        {
            var state = new ActiveCardState
            {
                CardId = cardId,
                InstanceId = Guid.NewGuid().ToString(),
                CurrentCharges = Mathf.Max(0, initialCharges),
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

        public bool TryConsumeCharge(string instanceId, int amount)
        {
            var st = GetCard(instanceId);
            if (st == null || amount <= 0) return false;
            if (st.CurrentCharges < amount) return false;

            st.CurrentCharges -= amount;
            NotifyChargesChanged(instanceId, st.CurrentCharges);
            return true;
        }

        public bool TryConsumeCharge(string instanceId, int amount, out int remaining)
        {
            remaining = 0;
            var st = GetCard(instanceId);
            if (st == null || amount <= 0) return false;
            if (st.CurrentCharges < amount)
            {
                remaining = st.CurrentCharges;
                return false;
            }
            st.CurrentCharges -= amount;
            remaining = st.CurrentCharges;
            NotifyChargesChanged(instanceId, st.CurrentCharges);
            return true;
        }

        public void AddCharges(string instanceId, int amount, int max)
        {
            var st = GetCard(instanceId);
            if (st == null || amount <= 0) return;

            int before = st.CurrentCharges;
            st.CurrentCharges = Mathf.Min(max, st.CurrentCharges + amount);

            if (before != st.CurrentCharges)
            {
                NotifyChargesChanged(instanceId, st.CurrentCharges);
            }
        }

        public void SetCharges(string instanceId, int charges, int? max = null)
        {
            var st = GetCard(instanceId);
            if (st == null) return;

            if (!max.HasValue)
            {
                var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
                max = cardDef?.activeCardConfig?.maxEnergy ?? 999;
            }

            int before = st.CurrentCharges;
            st.CurrentCharges = Mathf.Clamp(charges, 0, max.Value);

            if (before != st.CurrentCharges)
            {
                NotifyChargesChanged(instanceId, st.CurrentCharges);
            }
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
        /// 增加技能能量（自动触发 OnActiveCardChargesChanged 事件）
        /// </summary>
        public void AddEnergy(string instanceId, int amount)
        {
            var st = GetCard(instanceId);
            if (st == null || amount <= 0) return;

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            int maxEnergy = cardDef?.activeCardConfig?.maxEnergy ?? 999;

            int before = st.CurrentCharges;
            st.CurrentCharges = Mathf.Min(maxEnergy, st.CurrentCharges + amount);

            if (before != st.CurrentCharges)
            {
                NotifyChargesChanged(instanceId, st.CurrentCharges);
            }
        }

        /// <summary>
        /// 消耗技能能量（释放技能时调用，自动触发事件）
        /// </summary>
        public bool ConsumeSkillEnergy(string instanceId, bool consumeAll = true)
        {
            var st = GetCard(instanceId);
            if (st == null) return false;

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            if (cardDef?.activeCardConfig == null) return false;

            int before = st.CurrentCharges;

            if (consumeAll || cardDef.activeCardConfig.consumeAllEnergy)
            {
                st.CurrentCharges = 0;
            }
            else
            {
                int threshold = cardDef.activeCardConfig.energyThreshold;
                st.CurrentCharges = Mathf.Max(0, st.CurrentCharges - threshold);
            }

            if (before != st.CurrentCharges)
            {
                NotifyChargesChanged(instanceId, st.CurrentCharges);
            }

            return true;
        }

        /// <summary>
        /// 获取当前能量值
        /// </summary>
        public int GetCurrentEnergy(string instanceId)
        {
            return GetCard(instanceId)?.CurrentCharges ?? 0;
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

            int before = st.CurrentCharges;
            st.CurrentCharges = maxEnergy;

            if (before != st.CurrentCharges)
            {
                NotifyChargesChanged(instanceId, st.CurrentCharges);
            }
        }

        #endregion
    }
}
