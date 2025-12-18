using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using CardSystem.SkillSystem;
using CardSystem;
using System.Collections;
using CardSystem.SkillSystem.Runtime;

/// <summary>
/// 技能系统
/// </summary>
public class PlayerSkillComponent : MonoBehaviour, ISkillComponent
{
    [ReadOnly]
    [SerializeField]
    private SkillSlot[] _playerSkillSlots = new SkillSlot[2];

    public SkillSlot[] PlayerSkillSlots => _playerSkillSlots;
    // 保留事件签名以兼容现有绑定，但不触发任何事件
    public event Action<int, float> OnEnergyChanged;
    public event Action<int> OnSkillUsed;
    public event Action<int, string> OnSkillEquipped;
    public event Action<int> OnSkillUnequipped;
    private void Awake()
    {
        // 确保数组内的 SkillSlot 实例已初始化，避免 Inspector VS 运行期不一致
        if (_playerSkillSlots != null)
        {
            for (int i = 0; i < _playerSkillSlots.Length; i++)
            {
                if (_playerSkillSlots[i] == null)
                    _playerSkillSlots[i] = new SkillSlot();
            }
        }
    }
    /// <summary>
    /// 为指定槽位增加能量（可被外部如 PlayerManager 调用）
    /// 增加会同时触发 OnEnergyChanged 事件
    /// </summary>
    /// <param name="amount">增加量（直接加到 0-100 的区间）</param>
    public void AddEnergy(float amount)
    {
        if (_playerSkillSlots == null) return;
        for (int i = 0; i < _playerSkillSlots.Length; i++)
        {
            var slot = _playerSkillSlots[i];
            var rt = slot?.Runtime;
            if (rt == null) continue;

            float before = rt.Energy;
            rt.Energy = Mathf.Clamp(rt.Energy + amount, 0f, 100f);

            if (!Mathf.Approximately(before, rt.Energy))
            {
                OnEnergyChanged?.Invoke(i, rt.Energy);
            }
        }
    }

    public bool CanUseSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;
        var rt = _playerSkillSlots[slotIndex]?.Runtime;
        if (rt == null || rt.Skill == null) return false;
        // 房间内只能使用一次
        if (rt.UsedInCurrentRoom) return false;
        // 冷却判断
        var last = rt.LastUseTime;
        var baseCd = rt.Skill != null ? rt.Skill.cooldown : 0f;
        // 从角色属性获取冷却减速率（例如 0.2 表示冷却减少 20% -> 实际冷却 80%）
        var stats = GetComponent<CharacterStats>();
        var reduction = stats != null ? stats.SkillCooldownReductionRate.Value : 0f;
        var effectiveCd = Mathf.Max(0f, baseCd * (1f - reduction));
        if (Time.time - last < effectiveCd) return false;

        //TODO完善

        return true;
    }

    private IEnumerator DelayedExecute(SkillDefinition def, Vector3? aimPoint, int slotIndex)
    {
        if (def == null)
            yield break;

        // 等待检测延迟
        if (def.detectionDelay > 0f)
            yield return new WaitForSeconds(def.detectionDelay);

        // 计算目标点（若未指定，则使用施法者位置）
        Vector3 origin = aimPoint ?? transform.position;

        // 构建上下文 —— 目标检测/命中逻辑由 Executor 自行负责（Executor SO 包含其配置）
        var cardId = _playerSkillSlots != null && slotIndex >= 0 && slotIndex < _playerSkillSlots.Length ? _playerSkillSlots[slotIndex]?.Runtime?.CardId : null;

        var ctx = new SkillContext
        {
            Caster = GetComponent<Character.CharacterBase>(),
            Targets = null,
            AimPoint = origin,
            SlotIndex = slotIndex,
            CardId = cardId
        };

        // 播放 VFX（如果有）
        if (def.vfxPrefab != null)
        {
            try
            {
                GameObject.Instantiate(def.vfxPrefab, origin, Quaternion.identity);
            }
            catch { }
        }

        // 使用配置的 executor（若未指定，则默认直接应用 Effects 到 Targets）
        if (def.executor != null)
        {
            def.executor.Execute(def, ctx);
        }
        else
        {
            Debug.LogWarning($"[PlayerSkillComponent] Skill '{def.skillId}' has no executor configured. Skill will not apply effects. Please assign an Executor SO.");
        }

        yield break;
    }

    public void UseSkill(int slotIndex, Vector3? aimPoint = null)
    {
        if (!CanUseSkill(slotIndex)) return;

        var slot = _playerSkillSlots[slotIndex];
        var rt = slot?.Runtime;
        if (rt == null || rt.Skill == null) return;

        var def = rt.Skill;

        // 创建上下文占位（Targets 在实际执行时计算）
        var ctx = new SkillContext
        {
            Caster = GetComponent<Character.CharacterBase>(),
            Targets = null,
            AimPoint = aimPoint,
            SlotIndex = slotIndex,
            CardId = rt.CardId
        };

        // 启动协程处理（包含手动选择 / 消耗 / 冷却 / 延迟执行）
        StartCoroutine(ManualSelectAndExecute(def, ctx, slotIndex));
    }


    public void EquipActiveCardToSlotIndex(int slotIndex, string cardId)
    {
        // 不查 Inventory
        var cardDef = GameRoot.Instance.CardDatabase.Resolve(cardId);
        if (cardDef == null) return;

        var skillDef = cardDef.activeCardConfig.skill;
        if (_playerSkillSlots[slotIndex] == null)
            _playerSkillSlots[slotIndex] = new SkillSlot();
        _playerSkillSlots[slotIndex].Equip(new ActiveSkillRuntime(cardId, skillDef));
        OnSkillEquipped?.Invoke(slotIndex, cardId);
    }

    public void UnequipActiveCardBySlotIndex(int slotIndex)
    {
        var slot = _playerSkillSlots[slotIndex];
        if (slot == null) return;
        var cardId = slot.Runtime?.CardId;
        slot.Clear();
        OnSkillUnequipped?.Invoke(slotIndex);
    }

    /// <summary>
    /// 当进入新房间时RoomManager激活事件PlayerManager接收事件调用：重置本房间使用标记（但保留能量值）
    /// </summary>
    public void OnRoomEnter()
    {
        if (_playerSkillSlots == null) return;
        for (int i = 0; i < _playerSkillSlots.Length; i++)
        {
            var s = _playerSkillSlots[i];
            var rt = s?.Runtime;
            if (rt == null) continue;
            rt.UsedInCurrentRoom = false;
            // TODO-通知 UI 刷新（能量保留）
        }
    }

    /// <summary>
    /// 协程化的交互选择 + 执行流程：
    /// - 运行 targetingModule.ManualSelectionCoroutine(ctx)（供模块显示 UI / 高亮 / 等待点击）
    /// - 手动选择完成后尝试消耗卡牌充能（若卡片需要）
    /// - 标记冷却并执行技能（立即或延迟）
    /// </summary>
    private IEnumerator ManualSelectAndExecute(SkillDefinition def, SkillContext ctx, int slotIndex)
    {
        // 开始延迟检测/执行协程（若 detectionDelay 为 0，也会立即执行）
        StartCoroutine(DelayedExecute(def, ctx.AimPoint, slotIndex));

        yield break;
    }
}
