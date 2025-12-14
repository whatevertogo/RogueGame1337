using UnityEngine;
using UnityEngine.InputSystem;
using System;
using CDTU.Utils;

public class GameInput : Singleton<GameInput>
{
    private PlayerInputSystem playerInput;
    private bool hasLoggedZero = false; // 调试标志

    public Vector2 MoveDir { get; private set; }
    // 仅保留 Attack
    public bool AttackPressedThisFrame => playerInput != null && playerInput.PlayerControl.Attack.WasPerformedThisFrame();
    public bool AttackReleasedThisFrame => playerInput != null && playerInput.PlayerControl.Attack.WasReleasedThisFrame();
    public bool AttackIsPressed => playerInput != null && playerInput.PlayerControl.Attack.IsPressed();



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
            Debug.Log("🎯 Attack Pressed!");
        }
    }

    private void OnEnable()
    {
        if (playerInput == null)
        {
            playerInput = new PlayerInputSystem();
        }
        playerInput.Enable();
    }
    private void OnDisable() { playerInput?.Disable(); }
    private void OnDestroy() { playerInput?.Dispose(); }
}