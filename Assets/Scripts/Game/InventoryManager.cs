using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using RogueGame.Events;

/// <summary>
/// RunInventory: 运行时（单局）共享资源管理（金币、卡牌池）
/// 单例，供 UI/逻辑查询与 PlayerManager 转发
/// </summary>
public sealed class InventoryManager : Singleton<InventoryManager>
{

    [Serializable]
    public struct ActiveCardIdRuntimeInfo
    {
        public string cardId;
        public bool isEquipped;
        public string equippedPlayerId;
    }
    [Serializable]
    public struct PassiveCardIdRuntimeInfo
    {
        public string cardId;
        public int count;
    }
    [SerializeField] private List<ActiveCardIdRuntimeInfo> _ActiveCardIdInfos = new();
    [SerializeField] private List<PassiveCardIdRuntimeInfo> _PassiveCardIdInfos = new();

    // 运行时的主动卡牌完整状态列表（包含 instanceId / charges / cooldown / equip 状态）
    [Serializable]
    public class ActiveCardState
    {
        public string CardId;
        public string InstanceId;
        public int CurrentCharges;
        public bool IsEquipped;
        public string EquippedPlayerId;
        
        // 运行时专有字段（不序列化）
        [NonSerialized] public float CooldownRemaining;
    }

    private List<ActiveCardState> _activeCardStates = new List<ActiveCardState>();


    public IReadOnlyList<ActiveCardIdRuntimeInfo> ActiveCardIdInfos => _ActiveCardIdInfos;
    public IReadOnlyList<PassiveCardIdRuntimeInfo> PassiveCardIdInfos => _PassiveCardIdInfos;

    // 方便外部获取详细运行时状态（通过 instanceId）
    public IReadOnlyList<ActiveCardState> ActiveCardStates => _activeCardStates;

    public Action<string, int> OnActiveCardPoolChanged; // cardId, new count of instances in pool
    public Action<string, int> OnActiveCardChargesChanged; // instanceId, new charges

    [ReadOnly]
    [SerializeField] private int _coinsNumber = 0;
    public int CoinsNumber => _coinsNumber;
    public event Action<int> OnCoinsChanged;
    protected override void Awake()
    {
        base.Awake();
    }

    #region 卡牌操作

    public bool HasActiveCard(string cardId) => _ActiveCardIdInfos.Exists(info => info.cardId == cardId);


    // 创建一个新的主动卡牌运行时状态实例（返回 instanceId）
    public string AddActiveCardInstance(string cardId, int initialCharges = 0)
    {
        var st = new ActiveCardState
        {
            CardId = cardId,
            InstanceId = Guid.NewGuid().ToString(),
            CurrentCharges = Math.Max(0, initialCharges),
            CooldownRemaining = 0f,
            IsEquipped = false,
            EquippedPlayerId = null
        };

        _activeCardStates.Add(st);

        // 保持简化视图列表（仅 cardId + 装备信息）用于 UI 查询
        _ActiveCardIdInfos.Add(new ActiveCardIdRuntimeInfo { cardId = st.CardId, isEquipped = st.IsEquipped, equippedPlayerId = st.EquippedPlayerId });

        // notify pool changed (count of instances for this card)
        OnActiveCardPoolChanged?.Invoke(cardId, _activeCardStates.FindAll(s => s.CardId == cardId).Count);
        return st.InstanceId;
    }

    // 返回详细运行时状态（可为 null）
    public ActiveCardState GetActiveCardState(string instanceId)
    {
        return _activeCardStates.Find(s => s.InstanceId == instanceId);
    }

    // 返回第一个匹配的实例（或 null）
    public ActiveCardState GetFirstInstanceByCardId(string cardId)
    {
        return _activeCardStates.Find(s => s.CardId == cardId);
    }

    // 标记实例为被某玩家装备（playerId 可为空以取消装备）
    public void MarkInstanceEquipped(string instanceId, string playerId)
    {
        var st = GetActiveCardState(instanceId);
        if (st == null) return;
        st.IsEquipped = !string.IsNullOrEmpty(playerId);
        st.EquippedPlayerId = playerId;

        // 更新简化视图中的装备信息
        for (int i = 0; i < _ActiveCardIdInfos.Count; i++)
        {
            if (_ActiveCardIdInfos[i].cardId == st.CardId)
            {
                _ActiveCardIdInfos[i] = new ActiveCardIdRuntimeInfo { cardId = _ActiveCardIdInfos[i].cardId, isEquipped = st.IsEquipped, equippedPlayerId = st.EquippedPlayerId };
            }
        }

        OnActiveCardPoolChanged?.Invoke(st.CardId, _activeCardStates.FindAll(s => s.CardId == st.CardId).Count);
    }

    // 为指定玩家已装备的主动卡牌增加充能（通常在击杀事件中调用）
    public void AddChargesToEquippedPlayer(string playerId, int amount)
    {
        if (string.IsNullOrEmpty(playerId) || amount <= 0) return;
        foreach (var st in _activeCardStates)
        {
            if (st.EquippedPlayerId == playerId)
            {
                // 获取配置的上限
                var cd = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
                int max = cd != null ? cd.activeCardConfig.maxCharges : 1;
                int before = st.CurrentCharges;
                st.CurrentCharges = Math.Min(max, st.CurrentCharges + amount);
                if (st.CurrentCharges != before)
                {
                    OnActiveCardChargesChanged?.Invoke(st.InstanceId, st.CurrentCharges);
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
            if (st.EquippedPlayerId != playerId) continue;
            var cd = GameRoot.Instance?.CardDatabase?.Resolve(st.CardId);
            if (cd == null) continue;
            int baseGain = cd.activeCardConfig.chargesPerKill;
            int delta = Math.Max(0, baseGain * roomMult);
            int max = cd.activeCardConfig.maxCharges;
            int before = st.CurrentCharges;
            st.CurrentCharges = Math.Min(max, st.CurrentCharges + delta);
            if (st.CurrentCharges != before)
            {
                OnActiveCardChargesChanged?.Invoke(st.InstanceId, st.CurrentCharges);
            }
        }
    }

    // 消耗指定实例的充能，返回是否成功以及消耗后的剩余值
    public bool TryConsumeCharge(string instanceId, int amount, out int remaining)
    {
        remaining = 0;
        var st = GetActiveCardState(instanceId);
        if (st == null || amount <= 0) return false;
        if (st.CurrentCharges < amount) return false;
        st.CurrentCharges -= amount;
        remaining = st.CurrentCharges;
        OnActiveCardChargesChanged?.Invoke(st.InstanceId, st.CurrentCharges);
        return true;
    }

    #endregion

    #region 方便UI查询的卡牌池操作

    public List<ActiveCardIdRuntimeInfo> GetAllActiveCardIds()
    {
        return new List<ActiveCardIdRuntimeInfo>(_ActiveCardIdInfos);
    }

    public List<PassiveCardIdRuntimeInfo> GetAllPassiveCardIds()
    {
        return new List<PassiveCardIdRuntimeInfo>(_PassiveCardIdInfos);
    }

    public List<CardDefinition> GetAllActiveCardDefinitions()
    {
        var db = GameRoot.Instance?.CardDatabase;
        var list = new List<CardDefinition>();
        if (db == null) return list;
        foreach (var info in _ActiveCardIdInfos)
        {
            var cd = db.Resolve(info.cardId);
            if (cd != null)
            {
                list.Add(cd);
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] CardDefinition not found for active cardId='{info.cardId}'");
            }
        }
        return list;
    }

    public List<CardDefinition> GetAllPassiveCardDefinitions()
    {
        var db = GameRoot.Instance?.CardDatabase;
        var list = new List<CardDefinition>();
        if (db == null) return list;
        foreach (var info in _PassiveCardIdInfos)
        {
            var cd = db.Resolve(info.cardId);
            if (cd != null)
            {
                list.Add(cd);
            }
        }
        return list;
    }





    #endregion

    #region 金币操作
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        _coinsNumber += amount;
        OnCoinsChanged?.Invoke(_coinsNumber);
    }
    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (_coinsNumber < amount) return false;
        _coinsNumber -= amount;
        OnCoinsChanged?.Invoke(_coinsNumber);
        return true;
    }

    public void SetCoins(int amount)
    {
        if (amount < 0) amount = 0;
        _coinsNumber = amount;
        OnCoinsChanged?.Invoke(_coinsNumber);
    }

    #endregion


    #region Card操作

    public void AddPassiveCard(string cardId, int count = 1)
    {
        if (count <= 0) return;
        var info = _PassiveCardIdInfos.Find(i => i.cardId == cardId);
        if (info.cardId != null)
        {
            info.count += count;
            // 更新列表
            for (int i = 0; i < _PassiveCardIdInfos.Count; i++)
            {
                if (_PassiveCardIdInfos[i].cardId == cardId)
                {
                    _PassiveCardIdInfos[i] = info;
                    break;
                }
            }
        }
        else
        {
            _PassiveCardIdInfos.Add(new PassiveCardIdRuntimeInfo { cardId = cardId, count = count });
        }
    }

    public void AddActiveCard(string cardId)
    {
        var info = _ActiveCardIdInfos.Find(i => i.cardId == cardId);
        if (info.cardId == null)
        {
            _ActiveCardIdInfos.Add(new ActiveCardIdRuntimeInfo { cardId = cardId, isEquipped = false, equippedPlayerId = null });
        }
        else
        {
            // 已存在，无需重复添加
            //TODO-也许能加金币重复获得主动卡牌
            AddCoins(5);

        }
    }

    public void AddCardById(string cardId)
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;
        var cd = db.Resolve(cardId);
        if (cd == null) return;
        if (cd.CardType == CardType.Active)
        {
            AddActiveCard(cardId);
        }
        else if (cd.CardType == CardType.Passive)
        {
            AddPassiveCard(cardId, 1);
        }
    }




    #endregion

}
