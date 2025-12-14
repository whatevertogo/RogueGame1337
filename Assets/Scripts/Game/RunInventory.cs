using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;

/// <summary>
/// RunInventory: 运行时（单局）共享资源管理（金币、卡牌池）
/// 单例，供 UI/逻辑查询与 PlayerManager 转发
/// </summary>
public class RunInventory : Singleton<RunInventory>
{
    private int _coins = 0;
    public int Coins => _coins;

    // 被动卡池（全队共享）
    private readonly Dictionary<string, int> _passiveCards = new();
    // 主动卡池：默认为唯一卡（1 张），重复获取会转金币
    private readonly HashSet<string> _activeCards = new();
    // 已装备主动卡：cardId -> set of playerIds
    private readonly Dictionary<string, HashSet<string>> _activeCardEquippedBy = new();

    public event Action<int> OnCoinsChanged;
    public event Action<string, int> OnPassiveCardChanged; // (cardId, count)
    public event Action<string, int> OnActiveCardPoolChanged; // (cardId, count)

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

    public void AddPassiveCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;
        if (!_passiveCards.TryGetValue(cardId, out int count)) count = 0;
        count++;
        _passiveCards[cardId] = count;
        OnPassiveCardChanged?.Invoke(cardId, count);
    }

    /// <summary>
    /// 添加主动卡：若已有相同卡，则自动转换为金币（默认15）并返回 false
    /// 返回 true 表示卡牌已加入池中（可被装备）
    /// </summary>
    public bool AddActiveCard(string cardId, int duplicateConversionValue = 15)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        if (_activeCards.Contains(cardId))
        {
            AddCoins(duplicateConversionValue);
            return false;
        }
        _activeCards.Add(cardId);
        _activeCardEquippedBy[cardId] = new HashSet<string>();
        OnActiveCardPoolChanged?.Invoke(cardId, 1);
        return true;
    }

    /// <summary>
    /// 尝试装备主动卡：成功返回 true，失败（卡不存在/已被装备）返回 false
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="cardId"></param>
    /// <returns></returns>
    public bool TryEquipActive(string playerId, string cardId)
    {
        if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(cardId)) return false;
        if (!_activeCards.Contains(cardId)) return false;
        var set = _activeCardEquippedBy.ContainsKey(cardId) ? _activeCardEquippedBy[cardId] : null;
        if (set == null) return false;
        // active card pool is unique -> allow only one equip
        if (set.Count >= 1) return false;
        set.Add(playerId);
        OnActiveCardPoolChanged?.Invoke(cardId, 1 - set.Count);
        return true;
    }

    /// <summary>
    /// 卸下主动卡
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="cardId"></param>
    public void UnequipActive(string playerId, string cardId)
    {
        if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(cardId)) return;
        if (!_activeCards.Contains(cardId)) return;
        if (!_activeCardEquippedBy.TryGetValue(cardId, out var set)) return;
        if (set.Remove(playerId))
        {
            OnActiveCardPoolChanged?.Invoke(cardId, 1 - set.Count);
        }
    }

    /// <summary>
    /// 移除某玩家的所有已装备主动卡
    /// </summary>
    /// <param name="playerId"></param>
    public void RemoveAllEquipsForPlayer(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        foreach (var kv in _activeCardEquippedBy)
        {
            if (kv.Value.Remove(playerId))
            {
                OnActiveCardPoolChanged?.Invoke(kv.Key, 1 - kv.Value.Count);
            }
        }
    }

    public int GetPassiveCardCount(string cardId) => _passiveCards.TryGetValue(cardId, out var c) ? c : 0;
    public bool HasActiveCard(string cardId) => _activeCards.Contains(cardId);
}
