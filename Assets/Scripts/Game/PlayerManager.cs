using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueGame.Map;
using RogueGame.Events;
using CDTU.Utils;
using CardSystem.SkillSystem;

/// <summary>
/// 玩家管理器：负责玩家注册、共享库存转发、技能管理
/// 职责：
/// 1. 玩家生命周期管理（注册/注销）
/// 2. 共享库存事件转发（金币、卡牌）
/// 3. 技能系统协调（能量、使用状态）
/// 4. 事件总线集成
/// </summary>
public class PlayerManager : Singleton<PlayerManager>
{
    private readonly Dictionary<string, PlayerRuntimeState> _players = new();

    // 共享库存 (forwarded from RunInventory)
    public event Action<int> OnCoinsChanged;
    public event Action<string, int> OnPassiveCardChanged; // (cardId, count)
    public event Action<string, int> OnActiveCardPoolChanged; // (cardId, count)


    // 玩家注册事件
    public event Action<PlayerRuntimeState> OnPlayerRegistered;
    public event Action<PlayerRuntimeState> OnPlayerUnregistered;

    // 转发玩家技能事件（带 playerId）
    public event Action<string, int, float> OnPlayerSkillEnergyChanged; // (playerId, slotIndex, energy)
    public event Action<string, int> OnPlayerSkillUsed; // (playerId, slotIndex)
    public event Action<string, int, string> OnPlayerSkillEquipped; // (playerId, slotIndex, cardId)
    public event Action<string, int> OnPlayerSkillUnequipped; // (playerId, slotIndex)

    protected override void Awake()
    {
        base.Awake();

        // 订阅共享库存事件
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            inventoryManager.OnCoinsChanged += OnRunInventoryCoinsChanged;
            // ri.OnPassiveCardChanged += OnRunInventoryPassiveCardChanged;
            // ri.OnActiveCardPoolChanged += OnRunInventoryActiveCardPoolChanged;
        }

        // 订阅房间进入事件
        EventBus.Subscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
        // 订阅实体击杀事件，用于分发技能能量
        EventBus.Subscribe<EntityKilledEvent>(HandleEntityKilledEvent);
    }

    private void OnDestroy()
    {
        // 取消订阅共享库存事件
        var inventoryManager = InventoryManager.GetExistingInstance();
        if (inventoryManager != null)
        {
            inventoryManager.OnCoinsChanged -= OnRunInventoryCoinsChanged;
            // inventoryManager.OnPassiveCardChanged -= OnRunInventoryPassiveCardChanged;
            // inventoryManager.OnActiveCardPoolChanged -= OnRunInventoryActiveCardPoolChanged;
        }
        try
        {
            EventBus.Unsubscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
            EventBus.Unsubscribe<RogueGame.Events.EntityKilledEvent>(HandleEntityKilledEvent);
        }
        catch { }
    }

    private void HandleRoomEnteredEvent(RoomEnteredEvent evt)
    {
        // TODO-当房间被进入时，重置所有玩家的技能使用状态（仅本房间标记）
        ResetSkillUsageForAllPlayers();
    }

    private void HandleEntityKilledEvent(RogueGame.Events.EntityKilledEvent evt)
    {
        if (evt == null) return;
        NotifyEnemyKilled(evt.Attacker, evt.Victim, evt.RoomType);
    }

    private void ResetSkillUsageForAllPlayers()
    {
        foreach (var kv in _players)
        {
            var state = kv.Value;
            if (state?.Controller == null) continue;
            var comp = state.Controller.GetComponent<PlayerSkillComponent>();
            if (comp != null)
            {
                comp.OnRoomEnter();
            }
        }
    }

    #region 人物注册相关

    public PlayerRuntimeState RegisterPlayer(PlayerController controller, bool isLocal = true, string id = null)
    {
        if (controller == null) return null;
        if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString();
        if (_players.ContainsKey(id)) return _players[id];

        var state = new PlayerRuntimeState { PlayerId = id, Controller = controller, IsLocal = isLocal };
        _players[id] = state;

        // attach listeners for skill events
        AttachSkillListeners(state);

        OnPlayerRegistered?.Invoke(state);
        return state;
    }

    public void UnregisterPlayer(PlayerController controller)
    {
        if (controller == null) return;

        string key = null;
        foreach (var kv in _players)
            if (kv.Value.Controller == controller) { key = kv.Key; break; }

        if (key == null) return;
        var state = _players[key];


        // detach listeners
        DetachSkillListeners(state);

        _players.Remove(key);
        OnPlayerUnregistered?.Invoke(state);
    }

    public PlayerRuntimeState GetPlayerRuntimeStateByController(PlayerController controller)
    {
        if (controller == null) return null;
        foreach (var kv in _players) if (kv.Value.Controller == controller) return kv.Value;
        return null;
    }

    public PlayerRuntimeState GetPlayerRuntimeStateById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _players.TryGetValue(id, out var state);
        return state;
    }

    /// <summary>
    /// 获取所有玩家数据
    /// </summary>
    public IEnumerable<PlayerRuntimeState> GetAllPlayersData() => _players.Values;

    /// <summary>
    /// 获取本地玩家数据
    /// </summary>
    public PlayerRuntimeState GetLocalPlayerData()
    {
        return _players.Values.FirstOrDefault(p => p.IsLocal);
    }

    #endregion


    #region 技能转发事件
    
    private void AttachSkillListeners(PlayerRuntimeState state)
    {
        if (state == null || state.Controller == null) return;
        // instruct the controller to start forwarding skill events with its playerId
        state.Controller.StartSkillForwarding(this, state.PlayerId);
    }

    private void DetachSkillListeners(PlayerRuntimeState state)
    {
        if (state == null || state.Controller == null) return;
        // instruct the controller to stop forwarding skill events
        state.Controller.StopSkillForwarding();
    }

    #endregion
    
    #region 共享库存转发器

    private void OnRunInventoryCoinsChanged(int coins) => OnCoinsChanged?.Invoke(coins);
    private void OnRunInventoryPassiveCardChanged(string cardId, int count) => OnPassiveCardChanged?.Invoke(cardId, count);
    private void OnRunInventoryActiveCardPoolChanged(string cardId, int avail) => OnActiveCardPoolChanged?.Invoke(cardId, avail);

    public void AddCoins(PlayerController controller, int amount)
    {
        InventoryManager.Instance?.AddCoins(amount);
    }

    public bool SpendCoins(PlayerController controller, int amount)
    {
        return InventoryManager.Instance?.SpendCoins(amount) ?? false;
    }


    #endregion

    #region 通知 / 辅助方法

    #region skill forward raises

    // These methods allow other classes (e.g. PlayerSkillForwarder) to notify the manager
    // which will in turn invoke the events. Events cannot be invoked from outside the declaring class.
    internal void RaisePlayerSkillEnergyChanged(string playerId, int slotIndex, float energy)
    {
        OnPlayerSkillEnergyChanged?.Invoke(playerId, slotIndex, energy);
    }

    internal void RaisePlayerSkillUsed(string playerId, int slotIndex)
    {
        OnPlayerSkillUsed?.Invoke(playerId, slotIndex);
    }

    internal void RaisePlayerSkillEquipped(string playerId, int slotIndex, string cardId)
    {
        OnPlayerSkillEquipped?.Invoke(playerId, slotIndex, cardId);
    }

    internal void RaisePlayerSkillUnequipped(string playerId, int slotIndex)
    {
        OnPlayerSkillUnequipped?.Invoke(playerId, slotIndex);
    }

    #endregion

    /// <summary>
    /// 当敌人被击杀时通知，为击杀者添加能量
    /// </summary>
    public void NotifyEnemyKilled(GameObject attacker, GameObject enemy, RoomType roomType)
    {
        if (attacker == null || enemy == null) return;

        // 查找攻击者玩家
        PlayerController playerKiller = attacker.GetComponentInParent<PlayerController>();
        if (playerKiller == null)
        {
            // 尝试通过投射物查找拥有者
            var proj = attacker.GetComponent<Character.Projectiles.ProjectileBase>();
            if (proj?.Owner != null)
            {
                playerKiller = proj.Owner.GetComponentInParent<PlayerController>();
            }
        }

        if (playerKiller == null) return;

        // 根据房间类型给予不同能量奖励（数值可调整）
        float energy = roomType switch
        {
            RoomType.Elite => 30f,
            RoomType.Boss => 50f,
            _ => 10f
        };

        // 给击杀者添加能量（通知其 PlayerSkillComponent）
        var skillComp = playerKiller.GetComponent<PlayerSkillComponent>();
        if (skillComp != null)
        {
            skillComp.AddEnergy(energy);
        }
    }

    #endregion
}