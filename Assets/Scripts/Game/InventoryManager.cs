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
    // 运行时主动卡牌状态
    private List<ActiveCardState> _activeCardStates = new List<ActiveCardState>();
    public IReadOnlyList<ActiveCardState> ActiveCardStates => _activeCardStates;

    // 在主动卡牌充能变化时触发
    // (instanceId, currentCharges)
    public event Action<string, int> OnActiveCardChargesChanged;
    // 在主动卡牌池发生变化时触发（新增/移除/装备/卸下）
    // (cardId, availableInstanceCount)
    public event Action<string, int> OnActiveCardPoolChanged;

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

    // 创建一个新的主动卡牌运行时状态实例（返回 instanceId）
    public string AddActiveCardInstance(string cardId, int initialCharges = 0)
    {
        var st = new ActiveCardState
        {
            cardId = cardId,
            instanceId = Guid.NewGuid().ToString(),
            currentCharges = initialCharges,
            cooldownRemaining = 0f,
            equippedPlayerId = null
        };
        _activeCardStates.Add(st);
        // notify pool changed (count of instances for this card)
        OnActiveCardPoolChanged?.Invoke(cardId, _activeCardStates.FindAll(s => s.cardId == cardId).Count);
        return st.instanceId;
    }

    public ActiveCardState GetActiveCardInstance(string instanceId)
    {
        return _activeCardStates.Find(s => s.instanceId == instanceId);
    }

    public ActiveCardState GetFirstInstanceByCardId(string cardId)
    {
        return _activeCardStates.Find(s => s.cardId == cardId);
    }

    // 标记实例为被某玩家装备（playerId 可为空以取消装备）
    public void MarkInstanceEquipped(string instanceId, string playerId)
    {
        var st = GetActiveCardInstance(instanceId);
        if (st == null) return;
        st.equippedPlayerId = playerId;
        OnActiveCardPoolChanged?.Invoke(st.cardId, _activeCardStates.FindAll(s => s.cardId == st.cardId).Count);
    }

    // 为指定玩家已装备的主动卡牌增加充能（通常在击杀事件中调用）
    public void AddChargesToEquippedPlayer(string playerId, int amount)
    {
        if (string.IsNullOrEmpty(playerId) || amount <= 0) return;
        foreach (var st in _activeCardStates)
        {
            if (st.equippedPlayerId == playerId)
            {
                // 获取配置的上限
                var cd = GameRoot.Instance?.CardDatabase?.Resolve(st.cardId);
                int max = cd != null ? cd.activeCardConfig.maxCharges : 1;
                int before = st.currentCharges;
                st.currentCharges = Math.Min(max, st.currentCharges + amount);
                if (st.currentCharges != before)
                {
                    OnActiveCardChargesChanged?.Invoke(st.instanceId, st.currentCharges);
                }
            }
        }
    }

    // 根据击杀来源为该玩家已装备的主动卡片增加充能（利用卡牌配置的 chargesPerKill）
    public void AddChargesForKill(string playerId, RogueGame.Map.RoomType roomType)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        int roomMult = roomType == RogueGame.Map.RoomType.Elite ? 2 : (roomType == RogueGame.Map.RoomType.Boss ? 3 : 1);
        foreach (var st in _activeCardStates)
        {
            if (st.equippedPlayerId != playerId) continue;
            var cd = GameRoot.Instance?.CardDatabase?.Resolve(st.cardId);
            if (cd == null) continue;
            int baseGain = cd.activeCardConfig.chargesPerKill;
            int delta = Math.Max(0, baseGain * roomMult);
            int max = cd.activeCardConfig.maxCharges;
            int before = st.currentCharges;
            st.currentCharges = Math.Min(max, st.currentCharges + delta);
            if (st.currentCharges != before)
            {
                OnActiveCardChargesChanged?.Invoke(st.instanceId, st.currentCharges);
            }
        }
    }

    // 消耗指定实例的充能，返回是否成功以及消耗后的剩余值
    public bool TryConsumeCharge(string instanceId, int amount, out int remaining)
    {
        remaining = 0;
        var st = GetActiveCardInstance(instanceId);
        if (st == null || amount <= 0) return false;
        if (st.currentCharges < amount) return false;
        st.currentCharges -= amount;
        remaining = st.currentCharges;
        OnActiveCardChargesChanged?.Invoke(st.instanceId, st.currentCharges);
        return true;
    }




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
