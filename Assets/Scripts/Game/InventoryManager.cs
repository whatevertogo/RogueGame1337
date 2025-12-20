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

    private List<ActiveCardIdRuntimeInfo> _ActiveCardIdInfos = new List<ActiveCardIdRuntimeInfo>();
    private List<PassiveCardIdRuntimeInfo> _PassiveCardIdInfos = new List<PassiveCardIdRuntimeInfo>();

    // 运行时的主动卡牌完整状态列表（包含 instanceId / charges / cooldown / equip 状态）
    public class ActiveCardState
    {
        public string cardId;
        public string instanceId;
        public int currentCharges;
        public float cooldownRemaining;
        public bool isEquipped;
        public string equippedPlayerId;
    }

    private List<ActiveCardState> _activeCardStates = new List<ActiveCardState>();


    public IReadOnlyList<ActiveCardIdRuntimeInfo> ActiveCardIdInfos => _ActiveCardIdInfos;
    public IReadOnlyList<PassiveCardIdRuntimeInfo> PassiveCardIdInfos => _PassiveCardIdInfos;

    // 方便外部获取详细运行时状态（通过 instanceId）
    public IReadOnlyList<ActiveCardState> ActiveCardStates => _activeCardStates;

    public Action<string, int> OnActiveCardPoolChanged; // cardId, new count of instances in pool
    public Action<string, int> OnActiveCardChargesChanged; // instanceId, new charges

    [ReadOnly]
    [SerializeField] private int _coins = 0;
    public int Coins => _coins;
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
            cardId = cardId,
            instanceId = Guid.NewGuid().ToString(),
            currentCharges = Math.Max(0, initialCharges),
            cooldownRemaining = 0f,
            isEquipped = false,
            equippedPlayerId = null
        };

        _activeCardStates.Add(st);

        // 保持简化视图列表（仅 cardId + 装备信息）用于 UI 查询
        _ActiveCardIdInfos.Add(new ActiveCardIdRuntimeInfo { cardId = st.cardId, isEquipped = st.isEquipped, equippedPlayerId = st.equippedPlayerId });

        // notify pool changed (count of instances for this card)
        OnActiveCardPoolChanged?.Invoke(cardId, _activeCardStates.FindAll(s => s.cardId == cardId).Count);
        return st.instanceId;
    }

    // 返回详细运行时状态（可为 null）
    public ActiveCardState GetActiveCardState(string instanceId)
    {
        return _activeCardStates.Find(s => s.instanceId == instanceId);
    }

    // 返回第一个匹配的实例（或 null）
    public ActiveCardState GetFirstInstanceByCardId(string cardId)
    {
        return _activeCardStates.Find(s => s.cardId == cardId);
    }

    // 标记实例为被某玩家装备（playerId 可为空以取消装备）
    public void MarkInstanceEquipped(string instanceId, string playerId)
    {
        var st = GetActiveCardState(instanceId);
        if (st == null) return;
        st.isEquipped = !string.IsNullOrEmpty(playerId);
        st.equippedPlayerId = playerId;

        // 更新简化视图中的装备信息
        for (int i = 0; i < _ActiveCardIdInfos.Count; i++)
        {
            if (_ActiveCardIdInfos[i].cardId == st.cardId)
            {
                _ActiveCardIdInfos[i] = new ActiveCardIdRuntimeInfo { cardId = _ActiveCardIdInfos[i].cardId, isEquipped = st.isEquipped, equippedPlayerId = st.equippedPlayerId };
            }
        }

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
        var st = GetActiveCardState(instanceId);
        if (st == null || amount <= 0) return false;
        if (st.currentCharges < amount) return false;
        st.currentCharges -= amount;
        remaining = st.currentCharges;
        OnActiveCardChargesChanged?.Invoke(st.instanceId, st.currentCharges);
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
        }
        return list;
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
