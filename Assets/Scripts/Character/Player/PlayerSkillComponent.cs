using System;
using UnityEngine;
using Character.Components;

/// <summary>
/// 空技能组件（占位/降级实现）
/// 当用户选择删除/重写技能逻辑时，保留此组件以避免 Prefab/引用崩溃。
/// 该实现不包含任何业务逻辑——所有方法均为安全的 no-op 或返回默认值。
/// </summary>
public class PlayerSkillComponent : MonoBehaviour
{


	// 保留事件签名以兼容现有绑定，但不触发任何事件
	public event Action<int, float> OnEnergyChanged;
	public event Action<int> OnSkillUsed;
	public event Action<int, string> OnSkillEquipped;
	public event Action<int> OnSkillUnequipped;

	private void Awake()
	{
		
	}


}