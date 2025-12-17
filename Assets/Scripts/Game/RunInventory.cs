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
public sealed class RunInventory : Singleton<RunInventory>
{
    [ReadOnly]
    [SerializeField] private int _coins = 0;
    public int Coins => _coins;

    // 被动卡池（全队共享）
    private readonly Dictionary<string, int> _passiveCards = new();

    // 主动卡池：默认为唯一卡（1 张），重复获取会转金币
    private readonly HashSet<string> _activeSkillCards = new();

    // 已装备主动卡：cardId -> set of playerIds (按策略 A：卡为唯一时此集合最多包含一个 playerId)
    private readonly Dictionary<string, HashSet<string>> _activeCardEquippedBy = new();

    // 运行时主动卡状态：cardId -> ActiveCardState（例如 charges / cooldown / equippedPlayerId）
    [Serializable]
    public class ActiveCardState
    {
        public string cardId;
        public int currentCharges = 0;
        public float cooldownRemaining = 0f;
        public string equippedPlayerId = null;
    }

    private readonly Dictionary<string, ActiveCardState> _activeCardStates = new();

    /// <summary>
    /// 当主动卡的运行时 state 发生变化时触发（cardId, newState）
    /// UI / PlayerManager 可订阅以更新显示
    /// </summary>
    public event Action<string, ActiveCardState> OnActiveCardStateChanged;

    // 库存以 cardId 为主保持轻量（便于序列化/网络）
    // 编辑器模式下可编辑的序列化视图（Play Mode 下显示运行时实际集合）
    [Tooltip("编辑模式下用于配置主动技能卡（会同步到运行时主动卡池）")]
    [SerializeField] private List<CardIdReference> ActiveCards = new();

    [Tooltip("编辑模式下用于配置被动卡（会同步到运行时被动卡池）")]
    [SerializeField] private List<CardIdReference> PassiveCards = new();

    public event Action<int> OnCoinsChanged;
    public event Action<string, int> OnPassiveCardChanged; // (cardId, count)
    public event Action<string, int> OnActiveCardPoolChanged; // (cardId, count)

    protected override void Awake()
    {
        base.Awake();
        try
        {
            EventBus.Subscribe<EntityKilledEvent>(HandleEntityKilled);
        }
        catch { }
    }

    private void OnDestroy()
    {
        try
        {
            EventBus.Unsubscribe<EntityKilledEvent>(HandleEntityKilled);
        }
        catch { }
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
            // 初始化运行时 state（编辑器上可被同步）
            var data = CardRegistry.Resolve(cardId);
            var state = new ActiveCardState
            {
                cardId = cardId,
                currentCharges = data != null ? data.initialCharges : 0,
                cooldownRemaining = 0f,
                equippedPlayerId = null
            };
            _activeCardStates[cardId] = state;

            OnActiveCardPoolChanged?.Invoke(cardId, 1);
            OnActiveCardStateChanged?.Invoke(cardId, state);
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
    public bool TryEquipActiveCard(string cardId, string playerId, int slotIndex)
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

        // Enforce uniqueness (策略 A)：若已被其他玩家装备则拒绝（除非已是同一玩家）
        if (set.Count > 0 && !set.Contains(playerId))
        {
            // 已被他人装备
            return false;
        }

        // Idempotent: 如果该玩家已装备此卡，则视为成功（避免重复调用返回 false）
        if (set.Contains(playerId)) return true;

        var added = set.Add(playerId);
        if (added)
        {
            // 设置运行时 state 为已被该 player 装备
            if (_activeCardStates.TryGetValue(cardId, out var st))
            {
                st.equippedPlayerId = playerId;
                OnActiveCardStateChanged?.Invoke(cardId, st);
            }

            // 通知 PlayerManager 为 playerId 装备技能
            PlayerManager.Instance.EquipSkillToPlayer(playerId, data.skill, slotIndex);
        }
        return added;
    }

    /// <summary>
    /// 取消装备主动卡
    /// </summary>
    public bool UnequipActiveCard(string cardId, string playerId, int slotIndex)
    {
        if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(playerId)) return false;
        if (!_activeCardEquippedBy.TryGetValue(cardId, out var set)) return false;

        var removed = set.Remove(playerId);
        if (set.Count == 0)
        {
            _activeCardEquippedBy.Remove(cardId);
        }
        if (removed)
        {
            if (_activeCardStates.TryGetValue(cardId, out var st))
            {
                st.equippedPlayerId = null;
                OnActiveCardStateChanged?.Invoke(cardId, st);
            }
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

    /// <summary>
    /// 获取主动卡的运行时 state（如果存在）
    /// </summary>
    public ActiveCardState GetActiveCardState(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        _activeCardStates.TryGetValue(cardId, out var s);
        return s;
    }

    /// <summary>
    /// 尝试消耗主动卡的充能（成功返回 true）
    /// 必须匹配装备 playerId（以防止其他玩家消耗）
    /// </summary>
    public bool TryConsumeCharge(string cardId, string playerId, int amount = 1)
    {
        if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(playerId)) return false;
        if (!_activeCardStates.TryGetValue(cardId, out var s)) return false;
        if (s.equippedPlayerId != playerId) return false;
        if (s.currentCharges < amount) return false;
        s.currentCharges -= amount;
        OnActiveCardStateChanged?.Invoke(cardId, s);
        return true;
    }

    private void HandleEntityKilled(EntityKilledEvent evt)
    {
        if (evt == null || evt.Attacker == null) return;

        // 找到击杀者对应的 playerId
        PlayerController killer = evt.Attacker.GetComponentInParent<PlayerController>();
        if (killer == null)
        {
            var proj = evt.Attacker.GetComponent<Character.Projectiles.ProjectileBase>();
            if (proj != null && proj.Owner != null)
            {
                killer = proj.Owner.GetComponentInParent<PlayerController>();
            }
        }
        if (killer == null) return;

        var pm = PlayerManager.Instance;
        if (pm == null) return;
        var pstate = pm.GetPlayerRuntimeStateByController(killer);
        if (pstate == null) return;
        var playerId = pstate.PlayerId;

        foreach (var kv in _activeCardStates)
        {
            var cardId = kv.Key;
            var st = kv.Value;
            if (st.equippedPlayerId != playerId) continue;

            var data = CardRegistry.Resolve(cardId);
            if (data == null) continue;

            int inc = Math.Max(0, data.chargesPerKill);
            if (inc <= 0) continue;
            int max = Math.Max(1, data.maxCharges);

            int before = st.currentCharges;
            st.currentCharges = Math.Min(max, st.currentCharges + inc);
            if (st.currentCharges != before)
            {
                OnActiveCardStateChanged?.Invoke(cardId, st);
            }
        }
    }

    public int GetPassiveCardCount(string cardId) => _passiveCards.TryGetValue(cardId, out var c) ? c : 0;
    public bool HasActiveCard(string cardId) => _activeSkillCards.Contains(cardId);

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Only sync in edit-mode
        if (Application.isPlaying) return;

        try
        {
            // sync active
            _activeSkillCards.Clear();
            _activeCardStates.Clear();
            if (ActiveCards != null)
            {
                foreach (var r in ActiveCards)
                {
                    if (r == null) continue;
                    if (string.IsNullOrEmpty(r.Id)) continue;
                    _activeSkillCards.Add(r.Id);

                    // Editor-side: 初始化运行时 state（仅用于编辑器同步）
                    var data = CardRegistry.Resolve(r.Id);
                    var st = new ActiveCardState
                    {
                        cardId = r.Id,
                        currentCharges = data != null ? data.initialCharges : 0,
                        cooldownRemaining = 0f,
                        equippedPlayerId = null
                    };
                    _activeCardStates[r.Id] = st;
                }
            }

            // sync passive (counts)
            _passiveCards.Clear();
            if (PassiveCards != null)
            {
                foreach (var r in PassiveCards)
                {
                    if (r == null) continue;
                    if (string.IsNullOrEmpty(r.Id)) continue;
                    if (_passiveCards.ContainsKey(r.Id)) _passiveCards[r.Id]++;
                    else _passiveCards[r.Id] = 1;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"RunInventory OnValidate sync failed: {e.Message}");
        }
    }
#endif
}
