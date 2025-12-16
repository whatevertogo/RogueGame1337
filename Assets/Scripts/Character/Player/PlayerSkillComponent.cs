using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using CardSystem.SkillSystem;
using CardSystem;
using CardSystem.SkillSystem.Enum;
using Character;
using System.Collections;

/// <summary>
/// 空技能组件（占位/降级实现）
/// 当用户选择删除/重写技能逻辑时，保留此组件以避免 Prefab/引用崩溃。
/// 该实现不包含任何业务逻辑——所有方法均为安全的 no-op 或返回默认值。
/// </summary>
public class PlayerSkillComponent : MonoBehaviour, ISkillComponent
{
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
		var stats = GetComponent<Character.Components.CharacterStats>();
		var reduction = stats != null ? stats.SkillCooldownReductionRate.Value : 0f;
		var effectiveCd = Mathf.Max(0f, baseCd * (1f - reduction));
		return Time.time - last >= effectiveCd;
	}

	private IEnumerator DelayedExecute(SkillDefinition def, Vector3? aimPoint)
	{
		yield return new WaitForSeconds(def.detectionDelay);

		// 重新构建上下文并按照当前位置/目标重新采集
		var execCtx = new SkillContext(transform);
		if (def.targetingMode == SkillTargetingMode.Self)
		{
			execCtx.Targets.Add(gameObject);
		}
		else if (def.targetingMode == SkillTargetingMode.AOE)
		{
			var centre = aimPoint.HasValue ? aimPoint.Value : transform.position;
			execCtx.Position = centre;
			CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, def.radius, def.targetMask, execCtx.Targets,
				go => CardSystem.SkillSystem.Targeting.TargetingHelper.IsHostileTo(execCtx.OwnerTeam, go));
		}
		else if (def.targetingMode == SkillTargetingMode.SelfTarget)
		{
			var centre = transform.position;
			execCtx.Position = centre;
			CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, def.radius, def.targetMask, execCtx.Targets,
				go => go != gameObject);
		}

		// 最终执行
		def.Execute(execCtx);
		yield break;
	}

	public void UseSkill(int slotIndex, Vector3? aimPoint = null)
	{
		if (!CanUseSkill(slotIndex)) return;

		var slot = _playerSkillSlots[slotIndex];
		if (slot == null || slot.skill == null) return;

		var def = slot.skill;
		var ctx = new SkillContext(this.transform);

		// 解析目标
		if (def.targetingMode == SkillTargetingMode.Self)
		{
			ctx.Targets.Add(gameObject);
		}

		else if (def.targetingMode == SkillTargetingMode.AOE)
		{
			var centre = aimPoint.HasValue ? aimPoint.Value : transform.position;;
			ctx.Position = centre;
			// 使用 TargetingHelper 获取目标并按阵营过滤（立即采集，仅用于 preview；若 detectionDelay>0 会在执行时重新采集）
			CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, def.radius, def.targetMask, ctx.Targets,
				go => CardSystem.SkillSystem.Targeting.TargetingHelper.IsHostileTo(ctx.OwnerTeam, go));
		}
		else if (def.targetingMode == SkillTargetingMode.SelfTarget)
		{
			// SelfTarget: 以自身为中心的 AOE，但排除自身（可能用于治疗/增益盟友）
			var centre = transform.position;
			ctx.Position = centre;
			CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, def.radius, def.targetMask, ctx.Targets,
				go => go != gameObject);
		}

		// 标记冷却与事件（立即开始冷却以防止延迟期间重复释放）
		slot.lastUseTime = Time.time;
		OnSkillUsed?.Invoke(slotIndex);

		if (def.detectionDelay <= 0f)
		{
			// 立即执行
			def.Execute(ctx);
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


}