using UnityEngine;
using Character;
using Character.Core;

[RequireComponent(typeof(AutoPickupComponent))]
[RequireComponent(typeof(PlayerAnimator))]
public class PlayerController : CharacterBase
{
	// private Vector2 lastFacingDirection = Vector2.down;  // 记录上次朝向

	private PlayerAnimator playerAnim => GetComponent<PlayerAnimator>();

	private AutoPickupComponent autoPickup => GetComponent<AutoPickupComponent>();

	protected override void Awake()
	{
		base.Awake();

		// 订阅攻击事件（播放动画等）
		if (Combat != null)
		{
			Combat.OnAttack += OnAttackPerformed;
		}

		// 向 PlayerManager 注册自己（支持未来多人）
		var pm = PlayerManager.Instance;
		if (pm != null)
		{
			pm.RegisterPlayer(this, true);
		}

		var rb = GetComponent<Rigidbody2D>();
		var col = GetComponent<Collider2D>();
		Debug.Log($"[PlayerController] Awake: {gameObject.name}, tag={gameObject.tag}, layer={LayerMask.LayerToName(gameObject.layer)}, Rigidbody2D={(rb != null ? "Yes" : "No")}, Collider2D={(col != null ? "Yes" : "No")}");
	}

	//绑定到技能组件事件的处理程序（存储在控制器上，因此生命周期跟随游戏对象
	private System.Action<int, float> _boundEnergyChangedHandler;
	private System.Action<int> _boundSkillUsedHandler;

	/// <summary>
	/// 将外部处理器绑定到本玩家的技能组件，并在控制器中保存引用以便解除绑定。
	/// </summary>
	public void BindSkillHandlers(System.Action<int, float> energyHandler, System.Action<int> skillUsedHandler)
	{
		var skillComp = GetComponent<PlayerSkillComponent>();
		if (skillComp == null) return;

		// 先解除已有绑定以避免重复
		UnbindSkillHandlers();

		if (energyHandler != null)
		{
			skillComp.OnEnergyChanged += energyHandler;
			_boundEnergyChangedHandler = energyHandler;
		}
		if (skillUsedHandler != null)
		{
			skillComp.OnSkillUsed += skillUsedHandler;
			_boundSkillUsedHandler = skillUsedHandler;
		}
	}

	/// <summary>
	/// 解除插件绑定（安全可重入）。
	/// </summary>
	public void UnbindSkillHandlers()
	{
		var skillComp = GetComponent<PlayerSkillComponent>();
		if (skillComp == null) return;

		if (_boundEnergyChangedHandler != null)
		{
			skillComp.OnEnergyChanged -= _boundEnergyChangedHandler;
			_boundEnergyChangedHandler = null;
		}

		if (_boundSkillUsedHandler != null)
		{
			skillComp.OnSkillUsed -= _boundSkillUsedHandler;
			_boundSkillUsedHandler = null;
		}
	}

	// 在 PlayerController 内部维护的转发器类型，负责把技能事件转发给 PlayerManager
	private class PlayerSkillEventForwarder
	{
		private readonly PlayerManager owner;
		private readonly PlayerRuntimeState playerRuntimeState;
		public PlayerSkillEventForwarder(PlayerManager owner, PlayerRuntimeState playerRuntimeState)
		{
			this.owner = owner;
			this.playerRuntimeState = playerRuntimeState;
		}
		public void OnEnergyChanged(int slotIndex, float energy) => owner.ForwardSkillEnergyChanged(playerRuntimeState, slotIndex, energy);
		public void OnSkillUsed(int slotIndex) => owner.ForwardSkillUsed(playerRuntimeState, slotIndex);
	}

	/// <summary>
	/// 创建并绑定一个 PlayerSkillEventForwarder，以便 PlayerManager 将按玩家转发的事件接收并处理。
	/// </summary>
	public void BindSkillForwarder(PlayerManager owner, PlayerRuntimeState data)
	{
		if (owner == null || data == null) return;
		var forwarder = new PlayerSkillEventForwarder(owner, data);
		BindSkillHandlers(forwarder.OnEnergyChanged, forwarder.OnSkillUsed);
	}

	protected override void OnDestroy()
	{
		if (Combat != null)
		{
			Combat.OnAttack -= OnAttackPerformed;
		}
		// 注销
		var pm = PlayerManager.GetExistingInstance();
		if (pm != null)
		{
			pm.UnregisterPlayer(this);
		}
		base.OnDestroy();
	}

	private void Update()
	{
		if (Health != null && Health.IsDead) return;

		HandleMovementInput();
		HandleAttackInput();
	}

	private void HandleMovementInput()
	{
		Vector2 moveDir = GameInput.Instance.MoveDir;

		// 更新移动
		Movement?.SetInput(moveDir);

		//BUG: 先注释掉，未用以撒的攻击方式
		// // 记录朝向（用于攻击方向）
		// if (moveDir.sqrMagnitude > 0.01f)
		// {
		// 	lastFacingDirection = moveDir.normalized;
		// }

		// 更新动画
		UpdateAnimator(moveDir);
	}

	private void HandleAttackInput()
	{
		if (Combat == null) return;

		// 设置瞄准方向
		Vector2 aimDir = GetAimDirection();
		Combat.SetAim(aimDir);

		// 检测攻击输入
		if (GameInput.Instance.AttackIsPressed)
		{
			bool success = Combat.TryAttack();

			// if (success)
			// {
			// 	Debug.Log(" 攻击输入成功！");
			// }
			// else
			// {
			// 	Debug.Log($"攻击失败 - CanAttack: {Combat.CanAttack}, IsOnCooldown: {Combat.IsOnCooldown}, IsDisabled: {Combat.IsDisabled}");
			// }
		}
	}

	/// <summary>
	/// 获取瞄准方向
	/// </summary>
	private Vector2 GetAimDirection()
	{
		//方案一类似以撒
		// Vector2 moveDir = GameInput.Instance.MoveDir;

		// // 如果正在移动，用移动方向
		// if (moveDir.sqrMagnitude > 0.01f)
		// {
		// 	return moveDir.normalized;
		// }

		// // 否则用上次的朝向
		// return lastFacingDirection;

		//方案二以鼠标方向为准
		Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
		Vector2 aimDir = (mouseWorldPos - transform.position).normalized;
		return aimDir;
	}

	private void UpdateAnimator(Vector2 moveDir)
	{
		if (playerAnim != null)
		{
			bool isMoving = Movement?.IsMoving ?? false;
			playerAnim.SetMovement(moveDir, isMoving, false);
		}
	}

	private void OnAttackPerformed()
	{
		// Debug.Log("🔫 攻击动作执行！");

		// 播放攻击动画
		var playerAnim = GetComponent<PlayerAnimator>();
		playerAnim?.PlayAttack();
	}

	/// <summary>
	/// 处理技能激活通知
	/// slotIndex: 0 => Q, 1 => E
	/// </summary>
	/// <param name="slotIndex"></param>
	public void OnSkillActivated(int slotIndex)
	{
		Debug.Log($"[PlayerController] Skill activated request from PlayerManager: slot {slotIndex}");
		// TODO: 在这里触发技能系统（如果已实现）或播放技能动画
	}

	protected override void OnDeath()
	{
		base.OnDeath();
		Movement?.SetCanMove(false);
		Debug.Log("💀 玩家死亡");
	}
}