using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CDTU.Utils;
using RogueGame.Events;


/// <summary>
/// /// InventoryManager
/// - 只负责玩家库存状态
/// - 不包含技能、掉落、升级策略规则
/// - 不做任何随机或策划判断
/// </summary>
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

        /// <summary>
        /// 技能等级（Lv1-Lv5），用于主动技能升级系统
        /// </summary>
        public int Level = 1;

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
    /// 添加一个主动卡实例（内部方法，用于技能槽初始化等特殊场景）
    /// </summary>
    /// <param name="cardId"></param>
    /// <param name="initialCharges"></param>
    /// <returns></returns>
    private string AddActiveCardInstance(string cardId, int initialCharges)
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
    /// 内部方法：为技能槽系统创建卡牌实例（不触发升级/去重逻辑）
    /// 仅用于 PlayerSkillComponent 等内部系统初始化
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <param name="initialCharges">初始充能</param>
    /// <returns>实例ID</returns>
    public string CreateActiveCardInstanceInternal(string cardId, int initialCharges)
    {
        return AddActiveCardInstance(cardId, initialCharges);
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

    public void AddPassiveCard(string cardId, int count = 1, CardAcquisitionSource source = CardAcquisitionSource.Other)
    {
        if (count <= 0) return;

        // 获取被动卡叠加上限（从 StatLimitConfig）
        int maxStack = GameRoot.Instance?.StatLimitConfig?.maxPassiveCardStack ?? 99;

        for (int i = 0; i < _passiveCards.Count; i++)
        {
            if (_passiveCards[i].CardId == cardId)
            {
                int currentCount = _passiveCards[i].Count;
                int newCount = Mathf.Min(currentCount + count, maxStack);

                // 如果已经达到上限，记录警告
                if (currentCount >= maxStack)
                {
                    CDLogger.LogWarning($"[InventoryManager] 被动卡 {cardId} 已达到上限 ({maxStack})，无法继续叠加");
                    return;
                }

                int actualAdded = newCount - currentCount;
                _passiveCards[i] = new PassiveCardInfo
                {
                    CardId = cardId,
                    Count = newCount
                };

                // 发布被动卡拾取事件（使用实际添加的数量）
                EventBus.Publish(new PassiveCardAcquiredEvent(cardId, actualAdded, source));

                // 如果达到上限，记录日志
                if (newCount >= maxStack)
                {
                    CDLogger.Log($"[InventoryManager] 被动卡 {cardId} 已达到上限 ({maxStack})");
                }
                return;
            }
        }

        // 新卡牌，直接添加
        int actualCount = Mathf.Min(count, maxStack);
        _passiveCards.Add(new PassiveCardInfo
        {
            CardId = cardId,
            Count = actualCount
        });

        // 发布被动卡拾取事件
        EventBus.Publish(new PassiveCardAcquiredEvent(cardId, actualCount, source));
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

                // 发布被动卡移除事件
                EventBus.Publish(new PassiveCardRemovedEvent(cardId, count));
                return;
            }
        }
    }

    /// <summary>
    /// 获取指定被动卡牌的数量
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <returns>卡牌数量，如果没有则返回 0</returns>
    public int GetPassiveCardCount(string cardId)
    {
        foreach (var card in _passiveCards)
        {
            if (card.CardId == cardId)
            {
                return card.Count;
            }
        }
        return 0;
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

    /// <summary>
    /// 通过卡牌ID添加卡牌
    /// </summary>
    /// <param name="cardId"></param>
    public void AddCardById(string cardId)
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var def = db.Resolve(cardId);
        if (def == null) return;

        if (def.CardType == CardType.Active)
        {
            // 使用智能添加，支持去重和升级
            AddActiveCardSmart(cardId, def.activeCardConfig.energyPerKill);
        }
        else
        {
            AddPassiveCard(cardId, 1, CardAcquisitionSource.EnemyDrop);
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

    /// <summary>
    /// 从存档恢复时直接设置金币数（兼容旧 API）
    /// </summary>
    /// <param name="coins"></param>
    public void SetCoins(int coins)
    {
        _coins = Mathf.Max(0, coins);
        OnCoinsChanged?.Invoke(_coins);
    }

    /// <summary>
    /// 兼容旧名称：返回活动卡的运行时状态
    /// </summary>
    /// <param name="instanceId"></param>
    /// <returns></returns>
    public ActiveCardState GetActiveCardState(string instanceId) => GetActiveCard(instanceId);

    /// <summary>
    /// 查找第一个匹配 CardId 的主动卡实例（兼容旧 API）
    /// </summary>
    /// <param name="cardId"></param>
    /// <returns></returns>
    public ActiveCardState GetFirstInstanceByCardId(string cardId)
    {
        return _activeCards.Find(s => s != null && s.CardId == cardId);
    }

    /// <summary>
    /// 标记实例为已装备 / 取消装备（兼容旧 API）
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="playerId"></param>
    public void MarkInstanceEquipped(string instanceId, string playerId)
    {
        EquipActiveCard(instanceId, playerId);
    }

    /// <summary>
    /// 兼容旧 TryConsumeCharge overload：返回剩余充能数量
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="amount"></param>
    /// <param name="remaining"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 为玩家装备的主动卡发放击杀能量（符合新能量系统）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void AddChargesForKill(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        foreach (var st in _activeCards)
        {
            if (st == null || !st.IsEquipped || st.EquippedPlayerId != playerId) continue;
            var def = db.Resolve(st.CardId);
            if (def == null || def.activeCardConfig == null) continue;

            // 使用新的能量系统字段
            int energyAmount = def.activeCardConfig.energyPerKill;
            AddEnergy(st.InstanceId, energyAmount);
        }
    }

    // 兼容旧名称：CoinsNumber
    public int CoinsNumber => _coins;

    #endregion

    /// <summary>
    /// 随机获得一张主动卡牌（支持去重和升级）
    /// </summary>
    public void AddRandomActiveCard()
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var activeCardID = db.GetRandomCardId();

        // 使用智能添加，支持去重和升级
        AddActiveCardSmart(activeCardID, db.Resolve(activeCardID).activeCardConfig.energyPerKill);
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
    /// 直接设置充能值（用于重置等场景）
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="charges">目标充能值</param>
    /// <param name="max">最大充能上限（可选，不传则查询卡牌配置）</param>
    public void SetCharges(string instanceId, int charges, int? max = null)
    {
        var st = GetActiveCard(instanceId);
        if (st == null) return;

        // 如果没有指定上限，从卡牌配置获取
        if (!max.HasValue)
        {
            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            if (cardDef?.activeCardConfig != null)
            {
                max = cardDef.activeCardConfig.maxEnergy;
            }
            else
            {
                max = 999; // 默认上限，防止异常
            }
        }

        int before = st.CurrentCharges;
        st.CurrentCharges = Mathf.Clamp(charges, 0, max.Value);

        if (before != st.CurrentCharges)
        {
            OnActiveCardChargesChanged?.Invoke(instanceId, st.CurrentCharges);
        }
    }

    /// <summary>
    /// 增加能量（用于击杀敌人等场景）
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="amount">增加的能量值</param>
    public void AddEnergy(string instanceId, int amount)
    {
        var st = GetActiveCard(instanceId);
        if (st == null || amount <= 0) return;

        var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
        int maxEnergy = 999;

        if (cardDef?.activeCardConfig != null)
        {
            maxEnergy = cardDef.activeCardConfig.maxEnergy;
        }

        int before = st.CurrentCharges;
        st.CurrentCharges = Mathf.Min(maxEnergy, st.CurrentCharges + amount);

        if (before != st.CurrentCharges)
        {
            OnActiveCardChargesChanged?.Invoke(instanceId, st.CurrentCharges);
        }
    }

    /// <summary>
    /// 消耗技能能量（符合策划案：释放后清零）
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="consumeAll">是否清空所有能量（默认 true）</param>
    /// <returns>是否成功消耗</returns>
    public bool ConsumeSkillEnergy(string instanceId, bool consumeAll = true)
    {
        var st = GetActiveCard(instanceId);
        if (st == null) return false;

        var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
        if (cardDef?.activeCardConfig == null) return false;

        int before = st.CurrentCharges;

        // 根据配置决定消耗方式
        if (consumeAll || cardDef.activeCardConfig.consumeAllEnergy)
        {
            // 清空所有能量
            st.CurrentCharges = 0;
        }
        else
        {
            // 只减少能量阈值
            int threshold = cardDef.activeCardConfig.energyThreshold;
            st.CurrentCharges = Mathf.Max(0, st.CurrentCharges - threshold);
        }

        if (before != st.CurrentCharges)
        {
            OnActiveCardChargesChanged?.Invoke(instanceId, st.CurrentCharges);
        }

        return true;
    }

    #region 主动卡升级系统

    /// <summary>
    /// 获取指定卡牌的当前等级
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <returns>当前等级（1-5），如果没有该卡牌返回 0</returns>
    public int GetActiveCardLevel(string cardId)
    {
        var st = GetFirstInstanceByCardId(cardId);
        return st?.Level ?? 0;
    }

    /// <summary>
    /// 升级主动技能（如果存在且未达到最大等级）
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <param name="maxLevel">最大等级（如果为null，从 StatLimitConfig 读取）</param>
    /// <returns>升级后的等级，如果升级失败返回 -1</returns>
    public int UpgradeActiveCard(string cardId, int? maxLevel = null)
    {
        // 从配置读取最大等级（如果未指定）
        int actualMaxLevel = maxLevel ?? (GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel ?? 5);

        var st = GetFirstInstanceByCardId(cardId);
        if (st == null) return -1;

        if (st.Level >= actualMaxLevel)
        {
            // 已达到最大等级
            return st.Level;
        }

        st.Level++;
        OnActiveCardLevelUp?.Invoke(cardId, st.Level);

        CDLogger.Log($"[InventoryManager] 技能 '{cardId}' 升级至 Lv{st.Level}/{actualMaxLevel}");
        return st.Level;
    }

    /// <summary>
    /// 主动卡牌等级提升事件
    /// </summary>
    public event Action<string, int> OnActiveCardLevelUp;

    #endregion

    /// <summary>
    /// 智能添加主动卡（带去重检测和升级逻辑）
    /// 符合策划案：重复获得同一技能 → 自动升级
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <param name="initialCharges">初始充能值</param>
    /// <returns>返回值结构，包含是否添加、是否升级、等级等信息</returns>
    public ActiveCardAddResult AddActiveCardSmart(string cardId, int initialCharges)
    {
        var result = new ActiveCardAddResult
        {
            CardId = cardId,
            Success = false,
            Added = false,
            Upgraded = false,
            ConvertedToCoins = false,
            CoinsAmount = 0,
            NewLevel = 0
        };

        // 检查是否已存在该卡牌
        var existingCard = GetFirstInstanceByCardId(cardId);
        if (existingCard != null)
        {
            // 卡牌已存在，尝试升级（从 StatLimitConfig 读取最大等级）
            int oldLevel = existingCard.Level;
            int newLevel = UpgradeActiveCard(cardId, maxLevel: null); // null 表示从配置读取

            if (newLevel > oldLevel)
            {
                // 升级成功
                result.Success = true;
                result.Upgraded = true;
                result.NewLevel = newLevel;
                result.InstanceId = existingCard.InstanceId;

                CDLogger.Log($"[InventoryManager] 技能 '{cardId}' 升级：Lv{oldLevel} → Lv{newLevel}");
                return result;
            }
            else
            {
                // 已达到最大等级，根据配置决定行为
                var config = GetDeduplicationConfig();
                if (config != null && config.enableDeduplication)
                {
                    // 转换为金币
                    int coins = config.duplicateToCoins;
                    AddCoins(coins);

                    result.Success = true;
                    result.ConvertedToCoins = true;
                    result.CoinsAmount = coins;

                    if (config.showDeduplicationLog)
                    {
                        CDLogger.Log($"[InventoryManager] 技能 '{cardId}' 已达最大等级 Lv{GameRoot.Instance.StatLimitConfig?.maxActiveSkillLevel ?? 5}，转换为 {coins} 金币");
                    }
                    return result;
                }
                else
                {
                    // 不启用去重，无法添加
                    result.Success = false;
                    CDLogger.LogWarning($"[InventoryManager] 技能 '{cardId}' 已达最大等级且未启用去重转换");
                    return result;
                }
            }
        }

        // 卡牌不存在，添加新卡牌
        string instanceId = AddActiveCardInstance(cardId, initialCharges);
        result.Success = !string.IsNullOrEmpty(instanceId);
        result.Added = result.Success;
        result.InstanceId = instanceId;
        result.NewLevel = 1; // 新卡牌默认 Lv1

        if (result.Success)
        {
            CDLogger.Log($"[InventoryManager] 添加新技能 '{cardId}' (Lv1)");
        }

        return result;
    }

    /// <summary>
    /// 安全获取去重配置
    /// </summary>
    private ActiveCardDeduplicationConfig GetDeduplicationConfig()
    {
        if (GameRoot.Instance == null) return null;
        return GameRoot.Instance.ActiveCardDeduplicationConfig;
    }

    /// <summary>
    /// 智能添加随机主动卡（带去重检测和升级逻辑）
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

        return AddActiveCardSmart(activeCardID, cardDef.activeCardConfig.energyPerKill);
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

    /// <summary>是否升级了现有卡</summary>
    public bool Upgraded { get; set; }

    /// <summary>是否转换为金币</summary>
    public bool ConvertedToCoins { get; set; }

    /// <summary>卡牌ID</summary>
    public string CardId { get; set; }

    /// <summary>实例ID（Added 或 Upgraded 时有值）</summary>
    public string InstanceId { get; set; }

    /// <summary>新等级（Upgraded 时有值）</summary>
    public int NewLevel { get; set; }

    /// <summary>获得金币数量（仅当 ConvertedToCoins 为 true 时有值）</summary>
    public int CoinsAmount { get; set; }
}
