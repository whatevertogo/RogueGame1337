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

    protected override void Awake()
    {
        base.Awake();

        // 订阅共享库存事件
        var ri = RunInventory.Instance;
        if (ri != null)
        {
            ri.OnCoinsChanged += OnRunInventoryCoinsChanged;
            ri.OnPassiveCardChanged += OnRunInventoryPassiveCardChanged;
            ri.OnActiveCardPoolChanged += OnRunInventoryActiveCardPoolChanged;
        }

        // 订阅房间进入事件
        EventBus.Subscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
    }

    private void OnDestroy()
    {
        // 取消订阅共享库存事件
        var ri = RunInventory.GetExistingInstance();
        if (ri != null)
        {
            ri.OnCoinsChanged -= OnRunInventoryCoinsChanged;
            ri.OnPassiveCardChanged -= OnRunInventoryPassiveCardChanged;
            ri.OnActiveCardPoolChanged -= OnRunInventoryActiveCardPoolChanged;
        }


        try
        {
            EventBus.Unsubscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
        }
        catch { }
    }

    private void HandleRoomEnteredEvent(RoomEnteredEvent evt)
    {
        // TODO-当房间被进入时，重置所有玩家的技能使用状态（仅本房间标记）
        // ResetSkillUsageForAllPlayers();
    }

    #region 人物注册相关

    public PlayerRuntimeState RegisterPlayer(PlayerController controller, bool isLocal = true, string id = null)
    {
        if (controller == null) return null;
        if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString();
        if (_players.ContainsKey(id)) return _players[id];

        var state = new PlayerRuntimeState { PlayerId = id, Controller = controller, IsLocal = isLocal };
        _players[id] = state;

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

    #region 技能事件占位（待实现）

    public void EquipSkillToPlayer(string playerId, SkillDefinition skill, int slotIndex)
    {
        // TODO-实现为玩家装备技能的逻辑
        var playerRuntimeState = GetPlayerRuntimeStateById(playerId);
        if (playerRuntimeState == null)
        {
            Debug.LogWarning($"Player with ID {playerId} not found.");
            return;
        }
        var go = playerRuntimeState.Controller.gameObject;

        // 优先查找具体的 PlayerSkillComponent（有 EquipSkill/UnequipSkill）
        var playerSkillComp = go.GetComponent<PlayerSkillComponent>();
        if (playerSkillComp != null)
        {
            playerSkillComp.EquipSkill(skill, slotIndex);
            return;
        }
        else
        {
            //TODO-没问题了记得删掉
            CDTU.Utils.Logger.LogError($"No PlayerSkillComponent found on player {playerId}");
        }

    }

    public void UnequipSkillFromPlayer(string playerId, string cardId, int slotIndex)
    {
        var playerRuntimeState = GetPlayerRuntimeStateById(playerId);
        if (playerRuntimeState == null)
        {
            Debug.LogWarning($"Player with ID {playerId} not found.");
            return;
        }

        var go = playerRuntimeState.Controller.gameObject;
        var playerSkillComp = go.GetComponent<PlayerSkillComponent>();
        if (playerSkillComp != null)
        {
            playerSkillComp.UnequipSkill(slotIndex);
            return;
        }
        else
        {
            //TODO-没问题了记得删掉
            CDTU.Utils.Logger.LogError($"No PlayerSkillComponent found on player {playerId}");
        }
    }

    #endregion

    #region 共享库存转发器

    private void OnRunInventoryCoinsChanged(int coins) => OnCoinsChanged?.Invoke(coins);
    private void OnRunInventoryPassiveCardChanged(string cardId, int count) => OnPassiveCardChanged?.Invoke(cardId, count);
    private void OnRunInventoryActiveCardPoolChanged(string cardId, int avail) => OnActiveCardPoolChanged?.Invoke(cardId, avail);

    public void AddCoins(PlayerController controller, int amount)
    {
        RunInventory.Instance?.AddCoins(amount);
    }

    public bool SpendCoins(PlayerController controller, int amount)
    {
        return RunInventory.Instance?.SpendCoins(amount) ?? false;
    }


    #endregion

    #region 通知 / 辅助方法

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

        // 根据房间类型给予不同能量奖励
        float energy = roomType switch
        {
            RoomType.Elite => 30f,
            RoomType.Boss => 50f,
            _ => 10f
        };

        //TODO-给当前玩家添加能量逻辑
        // AddSkillEnergy(playerKiller, energy);
    }

    #endregion
}