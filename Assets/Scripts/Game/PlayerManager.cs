using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using RogueGame.Map;

/// <summary>
/// PlayerManager: registry and forwarder for shared inventory; handles per-player skill slots.
/// </summary>
public sealed class PlayerManager : Singleton<PlayerManager>
{
    private readonly Dictionary<string, PlayerRuntimeState> _players = new();

    public event Action<PlayerRuntimeState> OnPlayerRegistered;
    public event Action<PlayerRuntimeState> OnPlayerUnregistered;

    // Per-player skill events
    public event Action<PlayerRuntimeState, int, float> OnSkillEnergyChanged;
    public event Action<PlayerRuntimeState, int> OnSkillUsed;

    // 共享仓库 (forwarded from RunInventory)
    public event Action<int> OnSharedCoinsChanged;
    public event Action<string, int> OnSharedPassiveCardChanged;
    public event Action<string, int> OnSharedActiveCardPoolChanged;

    protected override void Awake()
    {
        base.Awake();
        var ri = RunInventory.Instance;
        if (ri != null)
        {
            // 使用具名方法订阅并在 OnDestroy 时反订阅，避免内存泄漏和便于调试
            ri.OnCoinsChanged += OnRunInventoryCoinsChanged;
            ri.OnPassiveCardChanged += OnRunInventoryPassiveCardChanged;
            ri.OnActiveCardPoolChanged += OnRunInventoryActiveCardPoolChanged;

            // 委托的方法
            OnSharedCoinsChanged += SharedCoinsChanged;
            OnSharedPassiveCardChanged += SharedPassiveCardChanged;
            OnSharedActiveCardPoolChanged += SharedActiveCardPoolChanged;
        }
    }

    // 注意方法签名必须匹配事件
    private void SharedCoinsChanged(int coins)
    {
        Debug.Log("金币改变：" + coins);
    }

    private void SharedPassiveCardChanged(string cardId, int count)
    {
        Debug.Log($"被动卡 {cardId} 数量改变：{count}");
    }

    private void SharedActiveCardPoolChanged(string cardId, int avail)
    {
        Debug.Log($"主动卡池 {cardId} 可用数量：{avail}");
    }

    // 转发 RunInventory 事件为 PlayerManager 事件（具名方法，方便取消订阅）
    private void OnRunInventoryCoinsChanged(int coins)
    {
        OnSharedCoinsChanged?.Invoke(coins);
    }

    private void OnRunInventoryPassiveCardChanged(string cardId, int count)
    {
        OnSharedPassiveCardChanged?.Invoke(cardId, count);
    }

    private void OnRunInventoryActiveCardPoolChanged(string cardId, int avail)
    {
        OnSharedActiveCardPoolChanged?.Invoke(cardId, avail);
    }

    void OnDestroy()
    {
        var ri = RunInventory.GetExistingInstance();
        if (ri != null)
        {
            ri.OnCoinsChanged -= OnRunInventoryCoinsChanged;
            ri.OnPassiveCardChanged -= OnRunInventoryPassiveCardChanged;
            ri.OnActiveCardPoolChanged -= OnRunInventoryActiveCardPoolChanged;
        }

        // 取消本地调试订阅
        OnSharedCoinsChanged -= SharedCoinsChanged;
        OnSharedPassiveCardChanged -= SharedPassiveCardChanged;
        OnSharedActiveCardPoolChanged -= SharedActiveCardPoolChanged;
    }


    #region 人物注册相关
    

    public PlayerRuntimeState RegisterPlayer(PlayerController controller, bool isLocal = true, string id = null)
    {
        if (controller == null) return null;
        if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString();
        if (_players.ContainsKey(id)) return _players[id];
        var state = new PlayerRuntimeState { PlayerId = id, Controller = controller, IsLocal = isLocal };
        _players[id] = state;
        // Subscribe to player's skill component events
        var skillComp = controller.GetComponent<PlayerSkillComponent>();
        if (skillComp != null)
        {
            // 由 PlayerController 创建并绑定转发器，以便生命周期与 GameObject 一致
            controller.BindSkillForwarder(this, state);
        }
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
        // 清理共享库存中的数据
        RunInventory.Instance?.RemoveAllEquipsForPlayer(state.PlayerId);
        // 通过 PlayerController 解绑处理器，使绑定状态跟随 GameObject
        controller.UnbindSkillHandlers();
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

    // 由 PlayerController 内部的 forwarder 调用，以把事件从 controller 转发到 PlayerManager 的事件链
    public void ForwardSkillEnergyChanged(PlayerRuntimeState data, int slotIndex, float energy) => OnSkillEnergyChanged?.Invoke(data, slotIndex, energy);
    public void ForwardSkillUsed(PlayerRuntimeState data, int slotIndex) => OnSkillUsed?.Invoke(data, slotIndex);

    public IEnumerable<PlayerRuntimeState> GetAllPlayersData() => _players.Values;
    #endregion

    #region 共享库存转发器
    public void AddCoins(PlayerController controller, int amount)
    {
        RunInventory.Instance?.AddCoins(amount);
    }

    public bool SpendCoins(PlayerController controller, int amount)
    {
        return RunInventory.Instance?.SpendCoins(amount) ?? false;
    }

    public void AddPassiveCard(PlayerController controller, string cardId)
    {
        RunInventory.Instance?.AddPassiveCard(cardId);
    }

    public void AddActiveSkillCard(PlayerController controller, string cardId)
    {
        RunInventory.Instance?.AddActiveCard(cardId);
    }
    #endregion

    #region 技能槽管理
    private const float SKILL_ENERGY_MAX = 100f;

    public float GetSkillEnergy(PlayerController controller, int slotIndex)
    {
        var p = GetPlayerRuntimeStateByController(controller);
        if (p == null) return 0f;
        if (slotIndex < 0 || slotIndex >= p.SkillSlots.Length) return 0f;
        return p.SkillSlots[slotIndex].Energy;
    }

    public void AddSkillEnergy(PlayerController controller, float energy)
    {
        var comp = controller.GetComponent<PlayerSkillComponent>();
        if (comp == null) return;
        comp.AddEnergy(energy);
    }

    public bool TryUseSkill(PlayerController controller, int slotIndex)
    {
        var comp = controller.GetComponent<PlayerSkillComponent>();
        if (comp == null) return false;
        return comp.TryUseSkillSlot(slotIndex);
    }

    public void ResetSkillUsageForAllPlayers()
    {
        foreach (var p in _players.Values) ResetSkillUsageForPlayer(p.Controller);
    }

    public void ResetSkillUsageForPlayer(PlayerController controller)
    {
        var p = GetPlayerRuntimeStateByController(controller);
        if (p == null) return;
        for (int i = 0; i < p.SkillSlots.Length; i++)
        {
            p.SkillSlots[i].UsedInRoom = false;
            OnSkillEnergyChanged?.Invoke(p, i, p.SkillSlots[i].Energy);
        }
    }

    public bool EquipSkillToSlot(PlayerController controller, string skillId, int slotIndex)
    {
        var comp = controller.GetComponent<PlayerSkillComponent>();
        if (comp == null) return false;
        return comp.EquipSkillSlot(slotIndex, skillId);
    }

    public void UnequipSkillFromSlot(PlayerController controller, int slotIndex)
    {
        var comp = controller.GetComponent<PlayerSkillComponent>();
        if (comp == null) return;
        comp.UnequipSkillSlot(slotIndex);
    }
    #endregion

    #region 通知 / 辅助方法
    // When RoomController detects enemy death and wants to notify, it can call this to add energy or other effects
    public void NotifyEnemyKilled(GameObject attacker, GameObject enemy, RoomType roomType)
    {
        if (attacker == null || enemy == null) return;

        PlayerController playerKiller = attacker.GetComponentInParent<PlayerController>();
        if (playerKiller == null)
        {
            var proj = attacker.GetComponent<Character.Projectiles.ProjectileBase>();
            if (proj != null && proj.Owner != null)
            {
                playerKiller = proj.Owner.GetComponentInParent<PlayerController>();
            }
        }

        if (playerKiller == null) return;
        float energy = roomType switch { RoomType.Elite => 30f, RoomType.Boss => 50f, _ => 10f };
        AddSkillEnergy(playerKiller, energy);
    }
    #endregion
}
