using System.Collections.Generic;
using CDTU.Utils;
using Core.Events;
using RogueGame.Events;
using RogueGame.Items;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 被动卡管理服务
    /// </summary>
    public class PassiveCardService
    {
        private readonly List<PassiveCardInfo> _passiveCards = new();

        public IReadOnlyList<PassiveCardInfo> Cards => _passiveCards;

        public void AddCard(string cardId, int count = 1, CardAcquisitionSource source = CardAcquisitionSource.Other)
        {
            if (count <= 0) return;

            int maxStack = GameRoot.Instance?.StatLimitConfig?.maxPassiveCardStack ?? 99;

            for (int i = 0; i < _passiveCards.Count; i++)
            {
                if (_passiveCards[i].CardId == cardId)
                {
                    int currentCount = _passiveCards[i].Count;
                    int newCount = UnityEngine.Mathf.Min(currentCount + count, maxStack);

                    if (currentCount >= maxStack)
                    {
                        CDLogger.LogWarning($"[PassiveCardService] {cardId} 已达上限 ({maxStack})");
                        return;
                    }

                    int actualAdded = newCount - currentCount;
                    _passiveCards[i] = new PassiveCardInfo { CardId = cardId, Count = newCount };
                    EventBus.Publish(new PassiveCardAcquiredEvent(cardId, actualAdded, source));

                    if (newCount >= maxStack)
                    {
                        CDLogger.Log($"[PassiveCardService] {cardId} 已达上限 ({maxStack})");
                    }
                    return;
                }
            }

            // 新卡牌
            int actualCount = UnityEngine.Mathf.Min(count, maxStack);
            _passiveCards.Add(new PassiveCardInfo { CardId = cardId, Count = actualCount });
            EventBus.Publish(new PassiveCardAcquiredEvent(cardId, actualCount, source));
        }

        public void RemoveCard(string cardId, int count = 1)
        {
            if (count <= 0) return;

            for (int i = 0; i < _passiveCards.Count; i++)
            {
                if (_passiveCards[i].CardId == cardId)
                {
                    int newCount = _passiveCards[i].Count - count;
                    if (newCount > 0)
                    {
                        _passiveCards[i] = new PassiveCardInfo { CardId = cardId, Count = newCount };
                    }
                    else
                    {
                        _passiveCards.RemoveAt(i);
                    }

                    EventBus.Publish(new PassiveCardRemovedEvent(cardId, count));
                    return;
                }
            }
        }

        public int GetCount(string cardId)
        {
            foreach (var card in _passiveCards)
            {
                if (card.CardId == cardId) return card.Count;
            }
            return 0;
        }

        public void Clear()
        {
            _passiveCards.Clear();
        }
    }
}
