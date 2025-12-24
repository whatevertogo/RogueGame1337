using UnityEngine;
using Character;
using Character.Player;
using UI;
using RogueGame.Events;

[RequireComponent(typeof(AutoPickupComponent))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerSkillComponent))]
public class PlayerController : CharacterBase
{
	// private Vector2 lastFacingDirection = Vector2.down;  // 记录上次朝向

	private PlayerAnimator playerAnim;
	private AutoPickupComponent autoPickup;
	private PlayerSkillComponent skillComponent;
	// 转发器实现：在控制器内部维护一个小型转发器类以避免使用 lambda
	private class PlayerSkillEventForwarder
	{
		private PlayerManager playerManager;
		private readonly string _playerId;

		public PlayerSkillEventForwarder(PlayerManager owner, string playerId)
		{
			_playerId = playerId;
			this.playerManager = owner;
		}

		public void OnEnergyChanged(int slotIndex, float energy)
		{
			playerManager?.RaisePlayerSkillEnergyChanged(_playerId, slotIndex, energy);
		}

		public void OnSkillUsed(int slotIndex)
		{
			playerManager?.RaisePlayerSkillUsed(_playerId, slotIndex);
		}

		public void OnSkillEquipped(int slotIndex, string cardId)
		{
			playerManager?.RaisePlayerSkillEquipped(_playerId, slotIndex, cardId);
		}

		public void OnSkillUnequipped(int slotIndex)
		{
			playerManager?.RaisePlayerSkillUnequipped(_playerId, slotIndex);
		}
	}

	private PlayerSkillEventForwarder _skillEventForwarder;
	private bool _skillForwardingActive = false;
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
		//以后多人写每个人应该注册id
		if (pm != null)
		{
			pm.RegisterPlayer(this, true);
		}

		//初始化组件
		var rb = GetComponent<Rigidbody2D>();
		var col = GetComponent<Collider2D>();
		playerAnim = GetComponent<PlayerAnimator>();
		autoPickup = GetComponent<AutoPickupComponent>();
		skillComponent = GetComponent<PlayerSkillComponent>();

		if (GameInput.Instance != null)
		{
			GameInput.Instance.OnSkillQPressed += () => TryActivateSkill(0); // 0 = Q技能槽
			GameInput.Instance.OnSkillEPressed += () => TryActivateSkill(1); // 1 = E技能槽
		}

		CDTU.Utils.Logger.Log($"[PlayerController] Awake: {gameObject.name}, tag={gameObject.tag}, layer={LayerMask.LayerToName(gameObject.layer)}, Rigidbody2D={(rb != null ? "Yes" : "No")}, Collider2D={(col != null ? "Yes" : "No")}");
	}

	 void OnEnable()
	{
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

	/// <summary>
	/// 将 PlayerSkillComponent 的事件转发到 PlayerManager，使用提供的 playerId。
	/// 由 PlayerManager 在玩家注册时调用。
	/// </summary>
	public void StartSkillForwarding(PlayerManager owner, string playerId)
	{
		if (_skillForwardingActive) StopSkillForwarding();
		if (skillComponent == null) skillComponent = GetComponent<PlayerSkillComponent>();
		if (skillComponent == null) return;

		// create nested forwarder and subscribe its instance methods (no lambdas)
		_skillEventForwarder = new PlayerSkillEventForwarder(owner, playerId);
		skillComponent.OnEnergyChanged += _skillEventForwarder.OnEnergyChanged;
		skillComponent.OnSkillUsed += _skillEventForwarder.OnSkillUsed;
		skillComponent.OnSkillEquipped += _skillEventForwarder.OnSkillEquipped;
		skillComponent.OnSkillUnequipped += _skillEventForwarder.OnSkillUnequipped;
		_skillForwardingActive = true;
	}

	/// <summary>
	/// Stop forwarding skill events and unsubscribe handlers.
	/// </summary>
	public void StopSkillForwarding()
	{
		if (!_skillForwardingActive) return;
		if (skillComponent != null && _skillEventForwarder != null)
		{
			skillComponent.OnEnergyChanged -= _skillEventForwarder.OnEnergyChanged;
			skillComponent.OnSkillUsed -= _skillEventForwarder.OnSkillUsed;
			skillComponent.OnSkillEquipped -= _skillEventForwarder.OnSkillEquipped;
			skillComponent.OnSkillUnequipped -= _skillEventForwarder.OnSkillUnequipped;
		}
		_skillEventForwarder = null;
		_skillForwardingActive = false;
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

		//更新人物朝向
		Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
		transform.localScale = new Vector3(mouseWorldPos.x > transform.position.x ? 1f : -1f, transform.localScale.y, transform.localScale.z);


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
			// 	CDTU.Utils.Logger.Log(" 攻击输入成功！");
			// }
			// else
			// {
			// 	CDTU.Utils.Logger.Log($"攻击失败 - CanAttack: {Combat.CanAttack}, IsOnCooldown: {Combat.IsOnCooldown}, IsDisabled: {Combat.IsDisabled}");
			// }
		}
	}


	public void TryActivateSkill(int slotIndex)
	{
		// 计算鼠标世界坐标作为瞄点，尝试找到显式目标（2D 优先），否则把瞄点传给技能
		Vector3 aimWorld = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
		aimWorld.z = 0f;
		// 我们使用范围伤害（AOE），不需要显式目标检测，直接把瞄点传给技能
		skillComponent.UseSkill(slotIndex, aimWorld);

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
			playerAnim.SetMovement(moveDir, isMoving);
		}
	}

	private void OnAttackPerformed()
	{
		// CDTU.Utils.Logger.Log("🔫 攻击动作执行！");

		// 播放攻击动画
		var playerAnim = GetComponent<PlayerAnimator>();
		playerAnim?.PlayAttack();
		// 攻击由Combat组件处理，这里只负责动画
	}

	protected override void OnDeath()
	{
		// 先取消任何正在进行的技能协程，避免死亡/复活冲突
		if (skillComponent != null)
		{
			skillComponent.CancelAllSkillCoroutines();
		}

		base.OnDeath();
		//无法移动
		Movement?.SetCanMove(false);

		//通知PlayerManager玩家死亡
		EventBus.Publish<PlayerDiedEvent>(new PlayerDiedEvent(this));

		// TODO- UIManager.Instance.Show<DeadUIView>
		// 播放死亡动画
		playerAnim.PlayDie();
		CDTU.Utils.Logger.Log("💀 玩家死亡");
	}

	private void OnDisable()
	{
		// 当控制器被禁用时也取消技能协程并停止转发
		if (skillComponent != null)
		{
			skillComponent.CancelAllSkillCoroutines();
		}
		StopSkillForwarding();
	}

	protected override void OnDamaged(float damage)
	{
		// 播放受伤动画
		playerAnim?.PlayHurt();

	}
}