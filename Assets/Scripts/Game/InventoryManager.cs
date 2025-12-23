using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CDTU.Utils;
using RogueGame.Map;

public sealed class InventoryManager : Singleton<InventoryManager>
{
    #region 数据结构

    [Serializable]
    public class ActiveCardState
    {
        public string CardId;
        public string InstanceId;
        public int CurrentCharges;
        public bool IsEquipped;
        public string EquippedPlayerId;

        [NonSerialized] public float CooldownRemaining;
    }

    [Serializable]
    public struct PassiveCardInfo
    {
        public string CardId;
        public int Count;
    }

    public struct ActiveCardView
    {
        public string CardId;
        public string InstanceId;
        public bool IsEquipped;
        public string EquippedPlayerId;
        public int Charges;
    }

    #endregion

    #region 内部状态

    [SerializeField, ReadOnly]
    private int _coins = 0;

    private readonly List<ActiveCardState> _activeCards = new();
    private readonly List<PassiveCardInfo> _passiveCards = new();

    #endregion

    #region 事件

    public event Action<int> OnCoinsChanged;

    public event Action<string> OnActiveCardInstanceAdded;          // instanceId
    public event Action<string, int> OnActiveCardChargesChanged;    // instanceId, charges
    public event Action<string> OnActiveCardEquipChanged;           // instanceId

    #endregion

    #region 对外只读访问

    public int Coins => _coins;

    public IReadOnlyList<ActiveCardState> ActiveCardStates => _activeCards;

    public IEnumerable<ActiveCardView> ActiveCardViews =>
        _activeCards.Select(st => new ActiveCardView
        {
            CardId = st.CardId,
            InstanceId = st.InstanceId,
            IsEquipped = st.IsEquipped,
            EquippedPlayerId = st.EquippedPlayerId,
            Charges = st.CurrentCharges
        });

    public IReadOnlyList<PassiveCardInfo> PassiveCards => _passiveCards;

    #endregion

    #region 金币

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

    #endregion

    #region 主动卡

    /// <summary>
    /// 添加一个主动卡实例
    /// </summary>
    /// <param name="cardId"></param>
    /// <param name="initialCharges"></param>
    /// <returns></returns>
    public string AddActiveCardInstance(string cardId, int initialCharges)
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

    /// <summary>
    /// 根据实例 ID 获取主动卡状态
    /// </summary>
    /// <param name="instanceId"></param>
    /// <returns></returns>
    public ActiveCardState GetActiveCard(string instanceId)
        => _activeCards.Find(c => c.InstanceId == instanceId);

    /// <summary>
    ///     为指定实例标记装备状态
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="playerId"></param>
    public void EquipActiveCard(string instanceId, string playerId)
    {
        var st = GetActiveCard(instanceId);
        if (st == null) return;

        st.IsEquipped = !string.IsNullOrEmpty(playerId);
        st.EquippedPlayerId = playerId;

        OnActiveCardEquipChanged?.Invoke(instanceId);
    }

    /// <summary>
    ///   尝试消耗指定实例的充能
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool TryConsumeCharge(string instanceId, int amount)
    {
        var st = GetActiveCard(instanceId);
        if (st == null || amount <= 0) return false;
        if (st.CurrentCharges < amount) return false;

        st.CurrentCharges -= amount;
        OnActiveCardChargesChanged?.Invoke(instanceId, st.CurrentCharges);
        return true;
    }

    /// <summary>
    /// 为指定实例添加充能
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="amount"></param>
    /// <param name="max"></param>
    public void AddCharges(string instanceId, int amount, int max)
    {
        var st = GetActiveCard(instanceId);
        if (st == null || amount <= 0) return;

        int before = st.CurrentCharges;
        st.CurrentCharges = Mathf.Min(max, st.CurrentCharges + amount);

        if (before != st.CurrentCharges)
        {
            OnActiveCardChargesChanged?.Invoke(instanceId, st.CurrentCharges);
        }
    }

    #endregion

    #region ===== 被动卡 =====

    public void AddPassiveCard(string cardId, int count = 1)
    {
        if (count <= 0) return;

        for (int i = 0; i < _passiveCards.Count; i++)
        {
            if (_passiveCards[i].CardId == cardId)
            {
                _passiveCards[i] = new PassiveCardInfo
                {
                    CardId = cardId,
                    Count = _passiveCards[i].Count + count
                };
                return;
            }
        }

        _passiveCards.Add(new PassiveCardInfo
        {
            CardId = cardId,
            Count = count
        });
    }

    #endregion

    #region ===== 相关 API =====

    /// <summary>
    /// 清除所有卡牌（主动卡和被动卡）
    /// </summary>
    public void ClearAllCards()
    {
        _activeCards.Clear();
        _passiveCards.Clear();
    }

    /// <summary>
    /// 扣除金币
    /// </summary>
    /// <param name="amount">要扣除的数量</param>
    public void RemoveCoins(int amount)
    {
        if (amount <= 0) return;
        _coins = Mathf.Max(0, _coins - amount);
        OnCoinsChanged?.Invoke(_coins);
    }

    /// <summary>
    /// 移除被动卡
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <param name="count">移除数量</param>
    public void RemovePassiveCard(string cardId, int count = 1)
    {
        if (count <= 0) return;

        for (int i = 0; i < _passiveCards.Count; i++)
        {
            if (_passiveCards[i].CardId == cardId)
            {
                int newCount = _passiveCards[i].Count - count;
                if (newCount > 0)
                {
                    _passiveCards[i] = new PassiveCardInfo
                    {
                        CardId = cardId,
                        Count = newCount
                    };
                }
                else
                {
                    _passiveCards.RemoveAt(i);
                }
                return;
            }
        }
    }

    /// <summary>
    /// 移除主动卡实例（通过 instanceId）
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    public void RemoveActiveCardInstance(string instanceId)
    {
        var st = GetActiveCard(instanceId);
        if (st != null)
        {
            _activeCards.Remove(st);
        }
    }

    /// <summary>
    /// 移除主动卡（通过 cardId，移除第一个匹配的实例）
    /// 注：主动卡通常只有唯一实例，但技术上可以有多份
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveActiveCardByCardId(string cardId)
    {
        var st = _activeCards.Find(c => c.CardId == cardId);
        if (st != null)
        {
            _activeCards.Remove(st);
            return true;
        }
        return false;
    }

    #endregion

    #region ===== 统一入口 =====

    public void AddCardById(string cardId)
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var def = db.Resolve(cardId);
        if (def == null) return;

        if (def.CardType == CardType.Active)
        {
            AddActiveCardInstance(cardId, def.activeCardConfig.chargesPerKill);
        }
        else
        {
            AddPassiveCard(cardId, 1);
        }
    }

    /// <summary>
    /// 通过卡牌ID移除卡牌
    /// - 主动卡：移除第一个匹配的实例（通常只有一个）
    /// - 被动卡：移除指定数量（默认1）
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    public void RemoveCardById(string cardId)
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var def = db.Resolve(cardId);
        if (def == null) return;

        if (def.CardType == CardType.Active)
        {
            // 主动卡：移除第一个匹配的实例
            RemoveActiveCardByCardId(cardId);
        }
        else
        {
            // 被动卡：移除1个
            RemovePassiveCard(cardId, 1);
        }
    }

    #endregion

    #region  兼容 API / 辅助方法




    // 从存档恢复时直接设置金币数（兼容旧 API）
    public void SetCoins(int coins)
    {
        _coins = Mathf.Max(0, coins);
        OnCoinsChanged?.Invoke(_coins);
    }

    // 兼容旧名称：返回活动卡的运行时状态
    public ActiveCardState GetActiveCardState(string instanceId) => GetActiveCard(instanceId);

    // 查找第一个匹配 CardId 的主动卡实例（兼容旧 API）
    public ActiveCardState GetFirstInstanceByCardId(string cardId)
    {
        return _activeCards.Find(s => s != null && s.CardId == cardId);
    }

    // 标记实例为已装备 / 取消装备（兼容旧 API）
    public void MarkInstanceEquipped(string instanceId, string playerId)
    {
        EquipActiveCard(instanceId, playerId);
    }

    // 兼容旧 TryConsumeCharge overload：返回剩余充能数量
    public bool TryConsumeCharge(string instanceId, int amount, out int remaining)
    {
        remaining = 0;
        var st = GetActiveCard(instanceId);
        if (st == null || amount <= 0) return false;
        if (st.CurrentCharges < amount)
        {
            remaining = st.CurrentCharges;
            return false;
        }
        st.CurrentCharges -= amount;
        remaining = st.CurrentCharges;
        OnActiveCardChargesChanged?.Invoke(instanceId, st.CurrentCharges);
        return true;
    }

    // 为玩家装备的主动卡发放击杀充能（RoomType 可作为未来权重依据）
    public void AddChargesForKill(string playerId, RoomType roomType)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        var db = GameRoot.Instance?.CardDatabase;
        var balance = GameRoot.Instance?.ChargeBalanceConfig;  // 获取配置
        if (db == null || balance == null) return;

        // 从配置获取充能值
        int chargeAmount = balance.GetChargeForRoomType(roomType);

        foreach (var st in _activeCards)
        {
            if (st == null || !st.IsEquipped || st.EquippedPlayerId != playerId) continue;
            var def = db.Resolve(st.CardId);
            if (def == null || def.activeCardConfig == null) continue;

            int max = Mathf.Max(1, def.activeCardConfig.maxCharges);
            AddCharges(st.InstanceId, chargeAmount, max);
        }
    }

    // 兼容旧名称：CoinsNumber
    public int CoinsNumber => _coins;

    #endregion

    /// <summary>
    /// 随机获得一张主动卡牌
    /// </summary>
    public void AddRandomActiveCard()
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var activeCardID = db.GetRandomCardId();

        AddActiveCardInstance(activeCardID, db.Resolve(activeCardID).activeCardConfig.chargesPerKill);
    }

    #region 主动卡去重

    /// <summary>
    /// 检查是否存在重复的主动卡
    /// </summary>
    /// <param name="cardId">要检查的卡牌ID</param>
    /// <returns>如果已存在相同ID的主动卡返回true</returns>
    public bool HasActiveCard(string cardId)
    {
        return _activeCards.Exists(c => c.CardId == cardId);
    }

    /// <summary>
    /// 获取已有主动卡的数量（按卡牌ID统计）
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <returns>该卡牌的实例数量</returns>
    public int GetActiveCardCount(string cardId)
    {
        return _activeCards.Count(c => c.CardId == cardId);
    }

    /// <summary>
    /// 智能添加主动卡（带去重检测）
    /// 如果卡牌已存在，则转换为金币；否则添加到库存
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <param name="initialCharges">初始充能值</param>
    /// <param name="dedupConfig">去重配置（可选，如果为null则使用默认配置）</param>
    /// <returns>返回值结构，包含是否添加、是否转换为金币等信息</returns>
    public ActiveCardAddResult AddActiveCardSmart(string cardId, int initialCharges)
    {
        var result = new ActiveCardAddResult
        {
            CardId = cardId,
            Success = false,
            Added = false,
            ConvertedToCoins = false,
            CoinsAmount = 0
        };


        // 检查是否启用去重
        if (GameRoot.Instance.ActiveCardDeduplicationConfig.enableDeduplication && HasActiveCard(cardId))
        {
            // 重复卡牌，转换为金币
            int coins = GameRoot.Instance.ActiveCardDeduplicationConfig.duplicateToCoins;
            AddCoins(coins);

            result.Success = true;
            result.ConvertedToCoins = true;
            result.CoinsAmount = coins;

            if (GameRoot.Instance.ActiveCardDeduplicationConfig.showDeduplicationLog)
            {
                Debug.Log($"[InventoryManager] 重复主动卡 '{cardId}' 已转换为 {coins} 金币");
            }
        }
        else
        {
            // 新卡牌，正常添加
            string instanceId = AddActiveCardInstance(cardId, initialCharges);

            result.Success = !string.IsNullOrEmpty(instanceId);
            result.Added = result.Success;
            result.InstanceId = instanceId;

            if (GameRoot.Instance.ActiveCardDeduplicationConfig.showDeduplicationLog && result.Success)
            {
                Debug.Log($"[InventoryManager] 添加主动卡 '{cardId}' (实例ID: {instanceId})");
            }
        }

        return result;
    }

    /// <summary>
    /// 智能添加随机主动卡（带去重检测）
    /// </summary>
    /// <param name="dedupConfig">去重配置（可选）</param>
    /// <returns>添加结果</returns>
    public ActiveCardAddResult AddRandomActiveCardSmart()
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null)
        {
            return new ActiveCardAddResult { Success = false };
        }

        var activeCardID = db.GetRandomCardId();
        var cardDef = db.Resolve(activeCardID);

        if (cardDef?.activeCardConfig == null)
        {
            return new ActiveCardAddResult { Success = false };
        }

        return AddActiveCardSmart(activeCardID, cardDef.activeCardConfig.chargesPerKill);
    }


    #endregion

}

/// <summary>
/// 主动卡添加结果
/// </summary>
public class ActiveCardAddResult
{
    /// <summary>是否成功处理</summary>
    public bool Success { get; set; }

    /// <summary>是否添加了新卡</summary>
    public bool Added { get; set; }

    /// <summary>是否转换为金币</summary>
    public bool ConvertedToCoins { get; set; }

    /// <summary>卡牌ID</summary>
    public string CardId { get; set; }

    /// <summary>实例ID（仅当Added为true时有值）</summary>
    public string InstanceId { get; set; }

    /// <summary>获得金币数量（仅当ConvertedToCoins为true时有值）</summary>
    public int CoinsAmount { get; set; }
}
