using System;
using RogueGame.Items;

namespace RogueGame.Game.Service.Inventory
{
    /// <summary>
    /// 金币管理服务
    /// </summary>
    public class CoinService
    {
        private int _coins = 0;

        public event Action<int> OnCoinsChanged;
        public int Coins => _coins;

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            _coins += amount;
            OnCoinsChanged?.Invoke(_coins);
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (_coins < amount) return false;
            _coins -= amount;
            OnCoinsChanged?.Invoke(_coins);
            return true;
        }

        public void RemoveCoins(int amount)
        {
            if (amount <= 0) return;
            _coins = UnityEngine.Mathf.Max(0, _coins - amount);
            OnCoinsChanged?.Invoke(_coins);
        }

        public void SetCoins(int coins)
        {
            _coins = UnityEngine.Mathf.Max(0, coins);
            OnCoinsChanged?.Invoke(_coins);
        }

        /// <summary>
        /// 只读访问（兼容旧 API）
        /// </summary>
        public int CoinsNumber => _coins;
    }
}
