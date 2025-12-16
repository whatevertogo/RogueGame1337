using UnityEngine;
using Character.Components;
using System;
public class PlayerSkillComponent : CharacterSkillComponent
{
    private PlayerController playerController;
    private PlayerRuntimeState playerRuntimeState;

    public event Action<int, float> OnEnergyChanged; // 槽位索引，新的能量值
    public event Action<int> OnSkillUsed; // 槽位索引

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
        if (playerController != null && PlayerManager.Instance != null)
        {
            // 尝试获取运行时状态；如果注册尚未发生，则订阅注册事件
            playerRuntimeState = PlayerManager.Instance.GetPlayerRuntimeStateByController(playerController);
            if (playerRuntimeState == null)
            {
                Debug.LogWarning("[PlayerSkillComponent] 未在Awake中为控制器 " + playerController.name + " 找到PlayerRuntimeState - 将等待注册。");
                PlayerManager.Instance.OnPlayerRegistered += OnPlayerRegistered;
            }
        }
    }

    private void Start()
    {
        // 以防万一，在Start中再次尝试获取PlayerRuntimeState
        if (playerRuntimeState == null && playerController != null && PlayerManager.Instance != null)
        {
            playerRuntimeState = PlayerManager.Instance.GetPlayerRuntimeStateByController(playerController);
            if (playerRuntimeState != null)
            {
                // 如果之前订阅了注册事件，现在取消订阅
                PlayerManager.Instance.OnPlayerRegistered -= OnPlayerRegistered;
            }
        }
    }

    private void OnDestroy()
    {
        var pm = PlayerManager.GetExistingInstance();
        if (pm != null)
        {
            pm.OnPlayerRegistered -= OnPlayerRegistered;
        }
    }

    private void OnPlayerRegistered(PlayerRuntimeState state)
    {
        if (state != null && state.Controller == playerController)
        {
            playerRuntimeState = state;
            // 不再需要监听
            PlayerManager.Instance.OnPlayerRegistered -= OnPlayerRegistered;
        }
    }

    /// <summary>
    /// 判断技能槽是否可用
    /// </summary>
    /// <param name="slotIndex">技能槽索引</param>
    /// <returns>是否可用</returns>
    public override bool CanUseSkill(int slotIndex)
    {
        // 可以在这里添加玩家特定的技能使用条件
        if (playerRuntimeState == null) return false;
        if (slotIndex < 0 || slotIndex >= playerRuntimeState.SkillSlots.Length) return false;
        return !playerRuntimeState.SkillSlots[slotIndex].UsedInRoom && playerRuntimeState.SkillSlots[slotIndex].Energy >= 100f;
    }

    /// <summary>
    /// 使用技能槽
    /// </summary>
    /// <param name="slotIndex">技能槽索引</param>
    public override void UseSkill(int slotIndex)
    {
        // 可以在这里添加玩家特定的技能使用逻辑
        if (!CanUseSkill(slotIndex)) return;
        // 消耗能量
        var slot = playerRuntimeState.SkillSlots[slotIndex];
        slot.Energy = 0f;
        slot.UsedInRoom = true;
        OnEnergyChanged?.Invoke(slotIndex, slot.Energy);
        OnSkillUsed?.Invoke(slotIndex);
        base.UseSkill(slotIndex);
    }

    /// <summary>
    /// 获取技能槽当前能量
    /// </summary>
    /// <param name="slotIndex">技能槽索引</param>
    /// <returns>当前能量</returns>
    public float GetSkillEnergy(int slotIndex)
    {
        if (playerRuntimeState == null) return 0f;
        if (slotIndex < 0 || slotIndex >= playerRuntimeState.SkillSlots.Length) return 0f;
        return playerRuntimeState.SkillSlots[slotIndex].Energy;
    }

    /// <summary>
    /// 为所有技能槽添加能量
    /// </summary>
    /// <param name="energy">要添加的能量值</param>
    public void AddEnergy(float energy)
    {
        if (playerRuntimeState == null || energy <= 0) return;
        for (int i = 0; i < playerRuntimeState.SkillSlots.Length; i++)
        {
            var slot = playerRuntimeState.SkillSlots[i];
            float before = slot.Energy;
            slot.Energy = Mathf.Clamp(slot.Energy + energy, 0f, 100f);
            if (Math.Abs(before - slot.Energy) > 0.01f)
            {
                OnEnergyChanged?.Invoke(i, slot.Energy);
            }
        }
    }

    /// <summary>
    /// 尝试使用技能槽：成功返回 true，失败返回 false
    /// </summary>
    /// <param name="slotIndex">技能槽索引</param>
    /// <returns>是否使用成功</returns>
    public bool TryUseSkillSlot(int slotIndex)
    {
        if (playerRuntimeState == null) return false;
        if (slotIndex < 0 || slotIndex >= playerRuntimeState.SkillSlots.Length) return false;
        if (playerRuntimeState.SkillSlots[slotIndex].UsedInRoom) return false;
        if (playerRuntimeState.SkillSlots[slotIndex].Energy < 100f) return false;
        UseSkill(slotIndex);
        return true;
    }

    /// <summary>
    /// 重置当前房间的技能使用状态
    /// </summary>
    public void ResetRoomUsage()
    {
        if (playerRuntimeState == null) return;
        for (int i = 0; i < playerRuntimeState.SkillSlots.Length; i++)
        {
            playerRuntimeState.SkillSlots[i].UsedInRoom = false;
            OnEnergyChanged?.Invoke(i, playerRuntimeState.SkillSlots[i].Energy);
        }
    }

    /// <summary>
    /// 装备技能槽
    /// </summary>
    /// <param name="slotIndex">技能槽索引</param>
    /// <param name="skillId">技能ID</param>
    /// <returns>是否装备成功</returns>
    public bool EquipSkillSlot(int slotIndex, string skillId)
    {
        if (playerRuntimeState == null) return false;
        if (slotIndex < 0 || slotIndex >= playerRuntimeState.SkillSlots.Length) return false;
        // 在RunInventory中预留主动技能卡
        if (!RunInventory.Instance.TryEquipActive(playerRuntimeState.PlayerId, skillId)) return false;
        playerRuntimeState.SkillSlots[slotIndex].EquippedSkillId = skillId;
        return true;
    }

    /// <summary>
    /// 卸下技能槽
    /// </summary>
    /// <param name="slotIndex">技能槽索引</param>
    public void UnequipSkillSlot(int slotIndex)
    {
        if (playerRuntimeState == null) return;
        if (slotIndex < 0 || slotIndex >= playerRuntimeState.SkillSlots.Length) return;
        var id = playerRuntimeState.SkillSlots[slotIndex].EquippedSkillId;
        if (string.IsNullOrEmpty(id)) return;
        RunInventory.Instance.UnequipActive(playerRuntimeState.PlayerId, id);
        playerRuntimeState.SkillSlots[slotIndex].EquippedSkillId = null;
    }
}