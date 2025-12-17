using System;
using System.Reflection;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using CardSystem.SkillSystem;
using CardSystem;
using CardSystem.SkillSystem.Enum;
using System.Collections;

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

    public bool CanUseSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;
        var slot = _playerSkillSlots[slotIndex];
        if (slot == null || slot.skill == null) return false;
        // 冷却判断
        var last = slot.lastUseTime;
        var baseCd = slot.skill != null ? slot.skill.cooldown : 0f;
        // 从角色属性获取冷却减速率（例如 0.2 表示冷却减少 20% -> 实际冷却 80%）
        var stats = GetComponent<CharacterStats>();
        var reduction = stats != null ? stats.SkillCooldownReductionRate.Value : 0f;
        var effectiveCd = Mathf.Max(0f, baseCd * (1f - reduction));
        if (Time.time - last < effectiveCd) return false;

        // 如果槽位关联了主动卡并且该卡需要消耗充能，则检查当前充能是否足够
        if (!string.IsNullOrEmpty(slot.cardId))
        {
            var cardData = CardRegistry.Resolve(slot.cardId);
            if (cardData != null && cardData.requiresCharge)
            {
                var state = RunInventory.Instance?.GetActiveCardState(slot.cardId);
                if (state == null || state.currentCharges <= 0) return false;
            }
        }

        return true;
    }

    private IEnumerator DelayedExecute(SkillDefinition def, Vector3? aimPoint)
    {
        yield return new WaitForSeconds(def.detectionDelay);

        // 重新构建上下文并按照当前位置/目标重新采集（传入 aimPoint 以兼容新的 TargetingModule）
        var execCtx = new SkillContext(transform);

        if (def.targetingModuleSO != null)
        {
            var mod = def.targetingModuleSO;
            var modType = mod.GetType();
            var requiresProp = modType.GetProperty("RequiresManualSelection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            bool requiresManual = false;
            if (requiresProp != null && requiresProp.PropertyType == typeof(bool))
            {
                var v = requiresProp.GetValue(mod);
                if (v is bool b) requiresManual = b;
            }
            if (!requiresManual)
            {
                var acquireMethod = modType.GetMethod("AcquireTargets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (acquireMethod != null)
                {
                    try
                    {
                        acquireMethod.Invoke(mod, new object[] { execCtx, aimPoint });
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[PlayerSkillComponent] AcquireTargets invoke failed: {ex.Message}");
                    }
                }
            }
        }

        // 最终执行，传入 aimPoint 以便 executionModule 使用（或 fallback）
        def.Execute(execCtx, aimPoint);
        yield break;
    }

    public void UseSkill(int slotIndex, Vector3? aimPoint = null)
    {
        if (!CanUseSkill(slotIndex)) return;

        var slot = _playerSkillSlots[slotIndex];
        if (slot == null || slot.skill == null) return;

        var def = slot.skill;
        var ctx = new SkillContext(this.transform);

        // 解析目标：若模块需要交互选择，则通过反射检查模块并在需要时启动专用协程处理交互与最终执行（包括消耗充能）
        if (def.targetingModuleSO != null)
        {
            var mod = def.targetingModuleSO;
            var modType = mod.GetType();

            // 检查 RequiresManualSelection（通过反射访问属性）
            bool requiresManual = false;
            var requiresProp = modType.GetProperty("RequiresManualSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (requiresProp != null && requiresProp.PropertyType == typeof(bool))
            {
                var val = requiresProp.GetValue(mod);
                if (val is bool b) requiresManual = b;
            }

            if (requiresManual)
            {
                // 交互式目标选择/确认由协程 ManualSelectAndExecute 负责（包括最终消耗与执行）
                StartCoroutine(ManualSelectAndExecute(def, ctx, slotIndex, aimPoint));
                return;
            }
            else
            {
                var acquireMethod = modType.GetMethod("AcquireTargets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (acquireMethod != null)
                {
                    try
                    {
                        acquireMethod.Invoke(mod, new object[] { ctx, aimPoint });
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[PlayerSkillComponent] AcquireTargets invoke failed: {ex.Message}");
                    }
                }
            }
        }
        // 非交互式流程：若槽位关联需要消耗充能的主动卡，则在此尝试消耗（交互式流程将在协程中再做消耗）
        if (!string.IsNullOrEmpty(slot.cardId))
        {
            var cdata = CardRegistry.Resolve(slot.cardId);
            if (cdata != null && cdata.requiresCharge)
            {
                var pm = PlayerManager.Instance;
                var pc = GetComponentInParent<PlayerController>();
                var pr = pm != null && pc != null ? pm.GetPlayerRuntimeStateByController(pc) : null;
                if (pr == null) return;
                var consumed = RunInventory.Instance?.TryConsumeCharge(slot.cardId, pr.PlayerId) ?? false;
                if (!consumed) return;
            }
        }

        // 标记冷却与事件（立即开始冷却以防止延迟期间重复释放）
        slot.lastUseTime = Time.time;
        OnSkillUsed?.Invoke(slotIndex);

        if (def.detectionDelay <= 0f)
        {
            // 立即执行（传入瞄点以便模块化目标选择使用）
            def.Execute(ctx, aimPoint);
        }
        else
        {
            // 延迟检测并执行：显示简单的预示 VFX（若配置），然后在协程中重新采集目标并执行
            // TODO-实现技能特效通过预制体
            if (def.vfxPrefab != null)
            {
                var tv = Instantiate(def.vfxPrefab, ctx.Position, Quaternion.identity);
                Destroy(tv, def.detectionDelay + 0.5f);
            }
            StartCoroutine(DelayedExecute(def, aimPoint));
        }

    }


    public void EquipSkill(SkillDefinition skill, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;

        if (_playerSkillSlots[slotIndex] == null)
            _playerSkillSlots[slotIndex] = new SkillSlot();

        // 如果槽位已有技能，先把它归还到仓库并触发卸下事件
        if (_playerSkillSlots[slotIndex].skill != null)
        {
            var old = _playerSkillSlots[slotIndex].skill;
            var oldCardId = FindCardIdForSkill(old);
            if (!string.IsNullOrEmpty(oldCardId))
            {
                RunInventory.Instance?.AddCardById(oldCardId);
            }
            OnSkillUnequipped?.Invoke(slotIndex);
        }

        _playerSkillSlots[slotIndex].skill = skill;

        // 触发装备事件，传递对应 cardId（如果能解析到）
        var newCardId = FindCardIdForSkill(skill);
        // 记录槽位关联的 cardId 以便运行时检查/消耗（例如 charges）
        _playerSkillSlots[slotIndex].cardId = newCardId;
        OnSkillEquipped?.Invoke(slotIndex, newCardId);
    }

    public void UnequipSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;
        var slot = _playerSkillSlots[slotIndex];
        if (slot == null || slot.skill == null) return;

        var oldSkill = slot.skill;
        var oldCardId = FindCardIdForSkill(oldSkill);
        if (!string.IsNullOrEmpty(oldCardId))
        {
            RunInventory.Instance?.AddCardById(oldCardId);
        }

        slot.skill = null;
        slot.cardId = null;
        OnSkillUnequipped?.Invoke(slotIndex);
    }

    private string FindCardIdForSkill(SkillDefinition skill)
    {
        if (skill == null) return null;
        foreach (var data in CardRegistry.GetAllDefinitions())
        {
            if (data?.skill == skill) return data.cardId;
        }
        return null;
    }

    /// <summary>
    /// 协程化的交互选择 + 执行流程：
    /// - 运行 targetingModule.ManualSelectionCoroutine(ctx)（供模块显示 UI / 高亮 / 等待点击）
    /// - 手动选择完成后尝试消耗卡牌充能（若卡片需要）
    /// - 标记冷却并执行技能（立即或延迟）
    /// </summary>
    private System.Collections.IEnumerator ManualSelectAndExecute(SkillDefinition def, SkillContext ctx, int slotIndex, Vector3? aimPoint)
    {
        if (def == null || def.targetingModuleSO == null) yield break;

        // 运行模块自带的交互协程（通过反射调用 ManualSelectionCoroutine，如果存在则等待它完成）
        if (def.targetingModuleSO != null)
        {
            var mod = def.targetingModuleSO;
            var method = mod.GetType().GetMethod("ManualSelectionCoroutine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                object obj = null;
                try
                {
                    obj = method.Invoke(mod, new object[] { ctx });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PlayerSkillComponent] ManualSelectionCoroutine invoke failed: {ex.Message}");
                }

                var enumerator = obj as System.Collections.IEnumerator;
                if (enumerator != null)
                {
                    yield return StartCoroutine(enumerator);
                }
            }
        }

        // 验证槽位与技能仍然有效
        if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) yield break;
        var slot = _playerSkillSlots[slotIndex];
        if (slot == null || slot.skill == null) yield break;

        // 交互完成后再尝试消耗充能（若需要）
        if (!string.IsNullOrEmpty(slot.cardId))
        {
            var cdata = CardRegistry.Resolve(slot.cardId);
            if (cdata != null && cdata.requiresCharge)
            {
                var pm = PlayerManager.Instance;
                var pc = GetComponentInParent<PlayerController>();
                var pr = pm != null && pc != null ? pm.GetPlayerRuntimeStateByController(pc) : null;
                if (pr == null) yield break;
                var consumed = RunInventory.Instance?.TryConsumeCharge(slot.cardId, pr.PlayerId) ?? false;
                if (!consumed) yield break;
            }
        }

        // 标记冷却与事件
        slot.lastUseTime = Time.time;
        OnSkillUsed?.Invoke(slotIndex);

        // 若有延迟：对手动选择的技能，不要调用 DelayedExecute（因为它会重新采集目标），
        // 而是使用已经选定的 ctx 并在等待后直接执行
        if (def.detectionDelay <= 0f)
        {
            def.Execute(ctx, aimPoint);
        }
        else
        {
            if (def.vfxPrefab != null)
            {
                // 若手动选择了显式目标，优先在该目标位置播放提示特效
                Vector3 spawnPos = ctx.Position;
                if (ctx.ExplicitTarget != null) spawnPos = ctx.ExplicitTarget.transform.position;
                var tv = Instantiate(def.vfxPrefab, spawnPos, Quaternion.identity);
                Destroy(tv, def.detectionDelay + 0.5f);
            }

            // 等待检测延迟后，再使用当前 ctx 直接执行技能（不重新采集目标）
            yield return new WaitForSeconds(def.detectionDelay);
            def.Execute(ctx, aimPoint);
        }

        yield break;
    }
}
