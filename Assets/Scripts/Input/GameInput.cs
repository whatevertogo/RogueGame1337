using UnityEngine;
using UnityEngine.InputSystem;
using System;
using CDTU.Utils;

public sealed class GameInput : Singleton<GameInput>
{
    private PlayerInputSystem playerInput;
    private readonly bool hasLoggedZero = false; // 调试标志

    public Vector2 MoveDir { get; private set; }
    // 仅保留 Attack
    public bool AttackPressedThisFrame => playerInput != null && playerInput.PlayerControl.Attack.WasPerformedThisFrame();
    public bool AttackReleasedThisFrame => playerInput != null && playerInput.PlayerControl.Attack.WasReleasedThisFrame();
    public bool AttackIsPressed => playerInput != null && playerInput.PlayerControl.Attack.IsPressed();

    public bool InteractPressedThisFrame => playerInput != null && playerInput.PlayerControl.Interact.WasPerformedThisFrame();
    public bool InteractReleasedThisFrame => playerInput != null && playerInput.PlayerControl.Interact.WasReleasedThisFrame();
    public bool InteractIsPressed => playerInput != null && playerInput.PlayerControl.Interact.IsPressed();

    // public bool SkillQPressedThisFrame => playerInput != null && playerInput.PlayerControl.SkillQ.WasPerformedThisFrame();
    // public bool SkillQReleasedThisFrame => playerInput != null && playerInput.PlayerControl.SkillQ.WasReleasedThisFrame();
    // public bool SkillQIsPressed => playerInput != null && playerInput.PlayerControl.SkillQ.IsPressed();

    // public bool SkillEPressedThisFrame => playerInput != null && playerInput.PlayerControl.SkillE.WasPerformedThisFrame();
    // public bool SkillEReleasedThisFrame => playerInput != null && playerInput.PlayerControl.SkillE.WasReleasedThisFrame();
    // public bool SkillEIsPressed => playerInput != null && playerInput.PlayerControl.SkillE.IsPressed(); 

    public event Action OnSkillQPressed;
    public event Action OnSkillEPressed;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("GameInput Awake called");
        playerInput = new PlayerInputSystem();
        playerInput.Enable();
        Debug.Log("PlayerInputSystem enabled");
    }

    private void Update()
    {
        MoveDir = playerInput != null ? playerInput.PlayerControl.Move.ReadValue<Vector2>() : Vector2.zero;


        //调试输出
        //调试攻击输入
        if (!hasLoggedZero) return;
        if (AttackPressedThisFrame)
        {
            Debug.Log("Attack Pressed!");
        }
    }

    private void OnEnable()
    {
        playerInput ??= new PlayerInputSystem();
        playerInput.Enable();
        playerInput.PlayerControl.SkillQ.performed += ctx => OnSkillQPressed?.Invoke();
        playerInput.PlayerControl.SkillE.performed += ctx => OnSkillEPressed?.Invoke();
    }
    private void OnDisable()
    {
        playerInput?.Disable();

        playerInput.PlayerControl.SkillQ.performed -= ctx => OnSkillQPressed?.Invoke();
        playerInput.PlayerControl.SkillE.performed -= ctx => OnSkillEPressed?.Invoke();
    }
    private void OnDestroy() { playerInput?.Dispose(); }
}