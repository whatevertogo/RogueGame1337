using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueGame.Map;
using RogueGame.Events;
using CDTU.Utils;
using Character.Player;
using UI;
using Game.UI;
using System.Threading.Tasks;

/// <summary>
/// 玩家管理器：负责玩家注册、共享库存转发、技能管理
/// 职责：
/// 1. 玩家生命周期管理（注册/注销）
/// 2. 事件总线集成
/// 
/// 
/// 相关服务：
/// - CombatRewardService：处理击杀奖励规则
/// - SkillChargeSyncService：负责库存 → 技能状态同步
/// </summary>
/// public CombatRewardService CombatRewardService => GameRoot.Instance.CombatRewardService;
/// public SkillChargeSyncService SkillChargeSyncService => GameRoot.Instance.SkillChargeSyncService;
public class PlayerManager : Singleton<PlayerManager>
{
    private readonly Dictionary<string, PlayerRuntimeState> _players = new();

    private PlayerLoader _playerLoader;

    private RoomManager RoomManager;


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

        // 订阅房间进入事件
        EventBus.Subscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
        // 订阅实体击杀事件，用于分发技能能量
        EventBus.Subscribe<EntityKilledEvent>(HandleEntityKilledEvent);
        // 订阅玩家死亡事件
        EventBus.Subscribe<PlayerDiedEvent>(HandlePlayerDiedEvent);
    }



    public void Initialize(RoomManager roomManager)
    {
        RoomManager = roomManager;
    }

    private void OnDestroy()
    {
        try
        {
            EventBus.Unsubscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
            EventBus.Unsubscribe<EntityKilledEvent>(HandleEntityKilledEvent);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerManager] 取消订阅事件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理房间进入事件
    /// 重置所有玩家的技能使用状态（仅本房间标记）
    /// </summary>
    /// <param name="evt"></param>
    private void HandleRoomEnteredEvent(RoomEnteredEvent evt)
    {
        ResetSkillUsageForAllPlayers();
    }



    /// <summary>
    /// 处理实体击杀事件
    /// 分发技能能量给击杀者
    /// <param name="evt"></param>
    /// </summary>
    private void HandleEntityKilledEvent(RogueGame.Events.EntityKilledEvent evt)
    {
        if (evt == null) return;
        NotifyEnemyKilled(evt.Attacker, evt.Victim, evt.RoomType);
    }

    /// <summary>
    /// 重置所有玩家的技能使用状态（仅本房间标记）
    /// </summary>
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

    /// <summary>
    /// 处理玩家死亡事件
    /// </summary>
    /// <param name="event"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void HandlePlayerDiedEvent(PlayerDiedEvent evt)
    {
        //TODO-处理玩家死亡后的逻辑，例如显示死亡UI等
        var playerState = GetPlayerRuntimeStateByController(evt.Player);

        //TODO 多人可能改

        //打开死亡UI
        if (playerState != null && playerState.IsLocal)
        {
            //TODO-传递死亡信息
            _ = OpenDeadUIAsync();
        }

    }

    public async Task OpenDeadUIAsync(string message = null)
    {
        try
        {
            Debug.Log("[PlayerManager] 正在打开死亡 UI...");

            await UIManager.Instance.Open<DeadUIView>(layer: UILayer.Popup);

            Debug.Log("[PlayerManager] 死亡 UI 打开成功");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerManager] 打开死亡 UI 失败: {ex.Message}");
        }
    }

    #region 公共api
    public PlayerRuntimeState GetLocalPlayerState()
    {
        return _players.Values.FirstOrDefault(p => p.IsLocal);
    }

    /// <summary>
    /// 创建玩家实例
    /// </summary>
    public void CreatePlayer()
    {
        _playerLoader ??= new PlayerLoader();
        var playerPrefab = _playerLoader.Load("Player1");
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerManager]无法加载玩家预制体。");
            return;
        }
        //TODO-以后在这里写LocalPlayer逻辑，现在是单人PlayerController自己设置为local
        var playerObj = GameObject.Instantiate(playerPrefab, RoomManager.GetCurrentRoomPosition(), Quaternion.identity);

    }
    #endregion  

    #region 人物注册相关

    /// <summary>
    /// 玩家自己注册到PlayerManager
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="isLocal"></param>
    /// <param name="id"></param>
    /// <returns></returns>
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


    #region 通知 / 辅助方法

    //转发器激活的方法

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

    public PlayerController ResolveAttacker(GameObject attacker)
    {
        if (attacker == null) return null;

        // 1. 直接来自玩家
        var player = attacker.GetComponent<PlayerController>();
        if (player != null) return player;

        // 2. 投射物来源
        var projectile = attacker.GetComponent<ProjectileBase>();
        if (projectile?.Owner == null) return null;

        return projectile.Owner.GetComponentInParent<PlayerController>();
    }

    /// <summary>
    /// 当敌人被击杀时通知，为击杀者添加能量
    /// </summary>
    public void NotifyEnemyKilled(GameObject attacker, GameObject enemy, RoomType roomType)
    {
        if (attacker == null || enemy == null) return;

        var playerKiller = ResolveAttacker(attacker);

        if (playerKiller == null) return;

        var prs = GetPlayerRuntimeStateByController(playerKiller);
        if (prs == null) return;

        GameRoot.Instance.CombatRewardEnergyService
            .GrantKillRewardEnergy(prs.PlayerId);
    }

    #endregion
}