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
    public struct ActiveCardIdRuntimeInfo
    {
        public string cardId;
    }

    public struct PassiveCardIdRuntimeInfo
    {
        public string cardId;
        public int count;
    }

    private List<ActiveCardIdRuntimeInfo> _ActiveCardIdInfos = new List<ActiveCardIdRuntimeInfo>();
    private List<PassiveCardIdRuntimeInfo> _PassiveCardIdInfos = new List<PassiveCardIdRuntimeInfo>();


    public IReadOnlyList<ActiveCardIdRuntimeInfo> ActiveCardIdInfos => _ActiveCardIdInfos;
    public IReadOnlyList<PassiveCardIdRuntimeInfo> PassiveCardIdInfos => _PassiveCardIdInfos;

    [ReadOnly]
    [SerializeField] private int _coins = 0;
    public int Coins => _coins;
    public event Action<int> OnCoinsChanged;
    protected override void Awake()
    {
        base.Awake();
    }

    private void OnDestroy()
    {
    }

    #region 卡牌操作

    public bool HasActiveCard(string cardId) => _ActiveCardIdInfos.Exists(info => info.cardId == cardId);




    #endregion

    #region 金币操作
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

    #endregion


}
