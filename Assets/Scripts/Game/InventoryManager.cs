using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using CardSystem;
using RogueGame.Events;

/// <summary>
/// RunInventory: 运行时（单局）共享资源管理（金币、卡牌池）
/// 单例，供 UI/逻辑查询与 PlayerManager 转发
/// </summary>
public sealed class InventoryManager : Singleton<InventoryManager>
{
    [ReadOnly]
    [SerializeField] private int _coins = 0;
    public int Coins => _coins;


    public class PassiveCardInfo
    {
        public string cardId;
        public int count;
    }
    public class ActiveCardInfo
    {
        public string cardId;
    }

    private readonly List<ActiveCardInfo> _activeCardInfos = new();
    private readonly List<PassiveCardInfo> _passiveCardInfos = new();

    public IReadOnlyList<ActiveCardInfo> ActiveCardIds => _activeCardInfos;
    public IReadOnlyList<PassiveCardInfo> PassiveCardIds => _passiveCardInfos;

    public event Action<int> OnCoinsChanged;
    protected override void Awake()
    {
        base.Awake();
    }

    private void OnDestroy()
    {
    }

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

    public void SetCoins(int amount)
    {
        if (amount < 0) amount = 0;
        _coins = amount;
        OnCoinsChanged?.Invoke(_coins);
    }



}
