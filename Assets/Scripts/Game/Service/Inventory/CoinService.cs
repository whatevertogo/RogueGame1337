using System;
using Core.Events;
using RogueGame.Events;
using RogueGame.Items;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 金币管理服务
    /// </summary>
    public class CoinService
    {
        private int _coins = 0;

        public int Coins => _coins;

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            _coins += amount;
            EventBus.Publish(new CoinTextUpdateEvent(_coins.ToString()));
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (_coins < amount) return false;
            _coins -= amount;
            EventBus.Publish(new CoinTextUpdateEvent(_coins.ToString()));
            return true;
        }

        public void RemoveCoins(int amount)
        {
            if (amount <= 0) return;
            _coins = UnityEngine.Mathf.Max(0, _coins - amount);
            EventBus.Publish(new CoinTextUpdateEvent(_coins.ToString()));
        }

        public void SetCoins(int coins)
        {
            _coins = UnityEngine.Mathf.Max(0, coins);
            EventBus.Publish(new CoinTextUpdateEvent(_coins.ToString()));
        }

        /// <summary>
        /// 只读访问（兼容旧 API）
        /// </summary>
        public int CoinsNumber => _coins;
    }
}
