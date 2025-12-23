using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CDTU.Utils;
using RogueGame.Map;

public sealed class InventoryManager : Singleton<InventoryManager>
{
    #region ===== 数据结构 =====

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

    #region ===== 内部状态 =====

    [SerializeField, ReadOnly]
    private int _coins = 0;

    private readonly List<ActiveCardState> _activeCards = new();
    private readonly List<PassiveCardInfo> _passiveCards = new();

    #endregion

    #region ===== 事件 =====

    public event Action<int> OnCoinsChanged;

    public event Action<string> OnActiveCardInstanceAdded;          // instanceId
    public event Action<string, int> OnActiveCardChargesChanged;    // instanceId, charges
    public event Action<string> OnActiveCardEquipChanged;           // instanceId

    #endregion

    #region ===== 对外只读访问 =====

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

    #region ===== 金币 =====

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

    #region ===== 主动卡 =====

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

    public ActiveCardState GetActiveCard(string instanceId)
        => _activeCards.Find(c => c.InstanceId == instanceId);

    public void EquipActiveCard(string instanceId, string playerId)
    {
        var st = GetActiveCard(instanceId);
        if (st == null) return;

        st.IsEquipped = !string.IsNullOrEmpty(playerId);
        st.EquippedPlayerId = playerId;

        OnActiveCardEquipChanged?.Invoke(instanceId);
    }

    public bool TryConsumeCharge(string instanceId, int amount)
    {
        var st = GetActiveCard(instanceId);
        if (st == null || amount <= 0) return false;
        if (st.CurrentCharges < amount) return false;

        st.CurrentCharges -= amount;
        OnActiveCardChargesChanged?.Invoke(instanceId, st.CurrentCharges);
        return true;
    }

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

    #endregion

    #region ===== 兼容 API / 辅助方法 =====

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
        var balance = GameRoot.Instance?.BalanceConfig;  // 获取配置
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
}
