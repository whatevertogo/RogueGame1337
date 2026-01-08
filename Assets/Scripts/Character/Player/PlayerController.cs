using Character;
using Character.Player;
using Core.Events;
using RogueGame.Events;
using UI;
using UnityEngine;

[RequireComponent(typeof(AutoPickupComponent))]
[RequireComponent(typeof(PlayerAnimatorController))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerSkillComponent))]
public class PlayerController : CharacterBase
{
    private PlayerAnimatorController playerAnim;
    private AutoPickupComponent autoPickup;
    private PlayerSkillComponent skillComponent;
    private Camera _mainCamera;

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
        playerAnim = GetComponent<PlayerAnimatorController>();
        autoPickup = GetComponent<AutoPickupComponent>();
        skillComponent = GetComponent<PlayerSkillComponent>();
        _mainCamera = Camera.main;

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnSkillQPressed += () => TryActivateSkill(0); // 0 = Q技能槽
            GameInput.Instance.OnSkillEPressed += () => TryActivateSkill(1); // 1 = E技能槽
            GameInput.Instance.OnSkillSpacePressed += () => TryActivateSkill(2); // 2 = 空格技能槽
        }

        CDTU.Utils.CDLogger.Log(
            $"[PlayerController] Awake: {gameObject.name}, tag={gameObject.tag}, layer={LayerMask.LayerToName(gameObject.layer)}, Rigidbody2D={(rb != null ? "Yes" : "No")}, Collider2D={(col != null ? "Yes" : "No")}"
        );
    }

    void OnEnable() { }

    protected override void OnDestroy()
    {
        if (Combat != null)
        {
            Combat.OnAttack -= OnAttackPerformed;
        }
        // 注销
        var pm = PlayerManager.Instance;
        if (pm != null)
        {
            pm.UnregisterPlayer(this);
        }
        base.OnDestroy();
    }

    private void Update()
    {
        if (Health != null && Health.IsDead)
            return;

        var mousePosition = MouseHelper2D.GetWorldPosition2D();

        HandleMovementInput(mousePosition);
        HandleAttackInput(mousePosition);
    }

    private void HandleMovementInput(Vector2 mousePosition)
    {
        Vector2 moveDir = GameInput.Instance.MoveDir;

        // 更新移动
        Movement?.SetInput(moveDir);

        //更新人物朝向
        float direction = mousePosition.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(
            direction,
            transform.localScale.y,
            transform.localScale.z
        );

        // 更新动画
        UpdateAnimator(moveDir);
    }

    private void HandleAttackInput(Vector2 mousePosition)
    {
        if (Combat == null)
            return;

        // 设置瞄准方向
        Vector2 aimDir = GetAimDirection(mousePosition);
        Combat.SetAim(aimDir);

        // 检测攻击输入
        if (GameInput.Instance.AttackIsPressed)
        {
            bool success = Combat.TryAttack();

            // if (success)
            // {
            // 	CDTU.Utils.CDLogger.Log(" 攻击输入成功！");
            // }
            // else
            // {
            // 	CDTU.Utils.CDLogger.Log($"攻击失败 - CanAttack: {Combat.CanAttack}, IsOnCooldown: {Combat.IsOnCooldown}, IsDisabled: {Combat.IsDisabled}");
            // }
        }
    }

    public void TryActivateSkill(int slotIndex)
    {
        Vector3 aimWorld = MouseHelper2D.GetWorldPosition();
        // 计算鼠标世界坐标作为瞄点，尝试找到显式目标，否则把瞄点传给技能
        aimWorld.z = 0f;
        // 我们使用范围伤害（AOE），不需要显式目标检测，直接把瞄点传给技能
        skillComponent.UseSkill(slotIndex, aimWorld);
    }

    /// <summary>
    /// 获取瞄准方向
    /// </summary>
    private Vector2 GetAimDirection(Vector2 mousePosition)
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
        Vector2 aimDir = (mousePosition - (Vector2)transform.position).normalized;
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
        var playerAnim = GetComponent<PlayerAnimatorController>();
        playerAnim?.PlayAttack();
        // 攻击由Combat组件处理，这里只负责动画
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        //无法移动
        Movement?.SetCanMove(false);

        //通知PlayerManager玩家死亡
        EventBus.Publish<PlayerDiedEvent>(new PlayerDiedEvent(this));

        // 播放死亡动画
        playerAnim.PlayDie();
        CDTU.Utils.CDLogger.Log("💀 玩家死亡");
    }

    private void OnDisable() { }

    protected override void OnDamaged(float damage)
    {
        // 播放受伤动画
        playerAnim?.PlayHurt();
    }

    public void EquipSkill(int slotIndex, string cardID)
    {
        skillComponent.EquipActiveCardToSlotIndex(slotIndex, cardID);
    }

    public void UnequipSkill(int slotIndex)
    {
        skillComponent.UnequipActiveCardBySlotIndex(slotIndex);
    }

    public void UnequipAllSkills()
    {
        for (int i = 0; i < skillComponent.SlotCount; i++)
        {
            skillComponent.UnequipActiveCardBySlotIndex(i);
        }
    }

    public void Interact() { }
}
