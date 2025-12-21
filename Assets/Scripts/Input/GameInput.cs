using UnityEngine;
using UnityEngine.InputSystem;
using System;
using CDTU.Utils;
using UnityEngine.XR;
using UI;

public sealed class GameInput : Singleton<GameInput>
{
    private PlayerInputSystem playerInput;

    public Vector2 MoveDir { get; private set; }
    // 仅保留 Attack
    public bool AttackPressedThisFrame => playerInput != null && playerInput.PlayerControl.Attack.WasPerformedThisFrame();
    public bool AttackReleasedThisFrame => playerInput != null && playerInput.PlayerControl.Attack.WasReleasedThisFrame();
    public bool AttackIsPressed => playerInput != null && playerInput.PlayerControl.Attack.IsPressed();

    public bool InteractPressedThisFrame => playerInput != null && playerInput.PlayerControl.Interact.WasPerformedThisFrame();
    public bool InteractReleasedThisFrame => playerInput != null && playerInput.PlayerControl.Interact.WasReleasedThisFrame();
    public bool InteractIsPressed => playerInput != null && playerInput.PlayerControl.Interact.IsPressed();

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

    }

    public void PausePlayerInput()
    {
        playerInput?.PlayerControl.Disable();
    }   
    public void ResumePlayerInput()
    {
        playerInput?.PlayerControl.Enable();
    }



    private void OnEnable()
    {
        playerInput ??= new PlayerInputSystem();
        playerInput.Enable();
        // 使用方法组订阅，便于正确取消订阅
        playerInput.PlayerControl.SkillQ.performed += OnSkillQPerformed;
        playerInput.PlayerControl.SkillE.performed += OnSkillEPerformed;
        playerInput.UI.ESC.performed += OnESCPerformed;
    }

    private void HandleESCPressed()
    {
        UIManager.Instance.HandleBack();
    }

    private void OnDisable()
    {
        playerInput?.Disable();
        if (playerInput != null)
        {
            playerInput.PlayerControl.SkillQ.performed -= OnSkillQPerformed;
            playerInput.PlayerControl.SkillE.performed -= OnSkillEPerformed;
            playerInput.UI.ESC.performed -= OnESCPerformed;
        }
    }

    // 输入回调
    private void OnESCPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        HandleESCPressed();
    }

    private void OnSkillQPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        OnSkillQPressed?.Invoke();
    }

    private void OnSkillEPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        OnSkillEPressed?.Invoke();
    }
    private void OnDestroy() { playerInput?.Dispose(); }
}