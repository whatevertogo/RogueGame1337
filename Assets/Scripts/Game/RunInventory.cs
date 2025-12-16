using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using CardSystem;

/// <summary>
/// RunInventory: 运行时（单局）共享资源管理（金币、卡牌池）
/// 单例，供 UI/逻辑查询与 PlayerManager 转发
/// </summary>
public sealed class RunInventory : Singleton<RunInventory>
{
    private int _coins = 0;
    public int Coins => _coins;

    // 被动卡池（全队共享）
    private readonly Dictionary<string, int> _passiveCards = new();
    // 主动卡池：默认为唯一卡（1 张），重复获取会转金币
    private readonly HashSet<string> _activeSkillCards = new();
    // 已装备主动卡：cardId -> set of playerIds
    private readonly Dictionary<string, HashSet<string>> _activeCardEquippedBy = new();
    // 库存以 cardId 为主保持轻量（便于序列化/网络）

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

    /// <summary>
    /// 通过 cardId 添加卡牌到库存（推荐）。库存以 cardId 为主，CardRegistry 提供元数据解析。
    /// </summary>
    public void AddCardById(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        var data = CardSystem.CardRegistry.Resolve(cardId);
        if (data != null)
        {
            if (data.type == CardType.Passive) AddPassiveCard(cardId);
            else AddActiveSkillCard(cardId);
        }
        else
        {
            Debug.LogWarning($"[RunInventory] AddCardById: unknown cardId '{cardId}'");
            AddActiveSkillCard(cardId);
        }
    }

    /// <summary>
    /// 添加被动卡到库存
    /// </summary>
    public void AddPassiveCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        if (_passiveCards.ContainsKey(cardId))
        {
            _passiveCards[cardId]++;
        }
        else
        {
            _passiveCards[cardId] = 1;
        }

        OnPassiveCardChanged?.Invoke(cardId, _passiveCards[cardId]);
    }

    /// <summary>
    /// 添加主动卡到库存
    /// </summary>
    /// <param name="cardId"></param>
    public void AddActiveSkillCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        // 如果池中没有该主动卡，则加入池并通知
        if (_activeSkillCards.Add(cardId))
        {
            OnActiveCardPoolChanged?.Invoke(cardId, 1);
        }
        else
        {
            // 已存在的重复主动卡：转换为金币（简单策略：+1 金币）
            AddCoins(1);
        }
    }

    /// <summary>
    /// 尝试将主动卡装备给玩家（仅当卡存在于池中时生效）
    /// </summary>
    public bool TryEquipActiveCard(string cardId, string playerId,int slotIndex)
    {
        if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(playerId)) return false;
        if (!_activeSkillCards.Contains(cardId)) return false;

        // 解析并校验
        var data = CardRegistry.Resolve(cardId);
        if (data == null)
        {
            Debug.LogWarning($"Unknown cardId {cardId}");
            return false;
        }
        if (data.type != CardType.Active)
        {
            Debug.LogWarning($"Card {cardId} is not Active");
            return false;
        }

        if (!_activeCardEquippedBy.TryGetValue(cardId, out var set))
        {
            set = new HashSet<string>();
            _activeCardEquippedBy[cardId] = set;
        }

        var added = set.Add(playerId);
        if (added)
        {
            // 通知 PlayerManager 为 playerId 装备技能
            PlayerManager.Instance.EquipSkillToPlayer(playerId, data.skill, slotIndex);
        }
        return added;
    }

    /// <summary>
    /// 取消装备主动卡
    /// </summary>
    public bool UnequipActiveCard(string cardId, string playerId,int slotIndex)
    {
        if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(playerId)) return false;
        if (!_activeCardEquippedBy.TryGetValue(cardId, out var set)) return false;

        var removed = set.Remove(playerId);
        if (set.Count == 0)
        {
            _activeCardEquippedBy.Remove(cardId);
        }
        if(removed)
        {
            PlayerManager.Instance.UnequipSkillFromPlayer(playerId, cardId, slotIndex);
        }
        return removed;
    }

    /// <summary>
    /// 获取已装备该主动卡的玩家 id 列表（只读副本）
    /// </summary>
    public IReadOnlyCollection<string> GetEquippedPlayers(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return Array.Empty<string>();
        if (_activeCardEquippedBy.TryGetValue(cardId, out var set))
        {
            return new List<string>(set).AsReadOnly();
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// 获取主动卡在池中的数量（目前为 0/1）
    /// </summary>
    public int GetActiveCardPoolCount(string cardId) => _activeSkillCards.Contains(cardId) ? 1 : 0;


    public int GetPassiveCardCount(string cardId) => _passiveCards.TryGetValue(cardId, out var c) ? c : 0;
    public bool HasActiveCard(string cardId) => _activeSkillCards.Contains(cardId);
}
