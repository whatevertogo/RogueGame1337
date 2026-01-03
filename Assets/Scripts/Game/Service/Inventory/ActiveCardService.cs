using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueGame.Items;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 主动卡核心管理服务
    /// </summary>
    public class ActiveCardService
    {
        private readonly List<ActiveCardState> _activeCards = new();

        public IReadOnlyList<ActiveCardState> ActiveCardStates => _activeCards;

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
            };

            _activeCards.Add(state);
            return state.InstanceId;
        }

        public ActiveCardState GetCardByInstanceId(string instanceId)
            => _activeCards.Find(c => c.InstanceId == instanceId);

        public ActiveCardState GetFirstByCardId(string cardId)
            => _activeCards.Find(s => s != null && s.CardId == cardId);
        
        public void RemoveInstance(string instanceId)
        {
            var st = GetCardByInstanceId(instanceId);
            if (st != null) _activeCards.Remove(st);
        }

        /// <summary>
        /// 根据卡号移除活动卡
        /// </summary>
        /// <param name="cardId">要移除的卡号</param>
        /// <returns>如果成功移除返回true，否则返回false</returns>
        public bool RemoveByCardId(string cardId)
        {
            // 在活动卡列表中查找指定卡号的卡
            var st = _activeCards.Find(c => c.CardId == cardId);
            // 如果找到匹配的卡
            if (st != null)
            {
                // 从活动卡列表中移除该卡
                _activeCards.Remove(st);
                // 返回true表示移除成功
                return true;
            }
            // 如果未找到匹配的卡，返回false
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

        public void AddEnergy(string instanceId, int energy)
        {
            var card = GetCardByInstanceId(instanceId);
            if (card != null)
            {
                card.CurrentEnergy += energy;
                if (card.CurrentEnergy < 0)
                    card.CurrentEnergy = 0;
            }
        }

        public bool ConsumeSkillEnergy(string instanceId, int energy)
        {
            var card = GetCardByInstanceId(instanceId);
            if (card != null && card.CurrentEnergy >= energy)
            {
                card.CurrentEnergy -= energy;
                return true;
            }
            return false;
        }
    }
}
