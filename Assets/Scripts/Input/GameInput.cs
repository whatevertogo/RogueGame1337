using System;
using CDTU.Utils;
using Game.UI;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public sealed class GameInput : Singleton<GameInput>
{
    private PlayerInputSystem playerInput;

    public Vector2 MoveDir { get; private set; }

    [ReadOnly]
    public Vector2 MoveDirRaw;

    // 仅保留 Attack
    public bool AttackPressedThisFrame =>
        playerInput != null && playerInput.PlayerControl.Attack.WasPerformedThisFrame();
    public bool AttackReleasedThisFrame =>
        playerInput != null && playerInput.PlayerControl.Attack.WasReleasedThisFrame();
    public bool AttackIsPressed =>
        playerInput != null && playerInput.PlayerControl.Attack.IsPressed();

    public bool InteractPressedThisFrame =>
        playerInput != null && playerInput.PlayerControl.Interact.WasPerformedThisFrame();
    public bool InteractReleasedThisFrame =>
        playerInput != null && playerInput.PlayerControl.Interact.WasReleasedThisFrame();
    public bool InteractIsPressed =>
        playerInput != null && playerInput.PlayerControl.Interact.IsPressed();

    public event Action OnSkillQPressed;
    public event Action OnSkillEPressed;
    public event Action OnSkillSpacePressed;

    protected override void Awake()
    {
        base.Awake();
        CDLogger.Log("GameInput Awake called");

        try
        {
            playerInput = new PlayerInputSystem();
            playerInput.Enable();
            CDLogger.Log("PlayerInputSystem enabled successfully");
        }
        catch (System.Exception ex)
        {
            CDLogger.LogError($"[GameInput] 初始化输入系统失败: {ex.Message}");
            // 尝试继续运行，但记录错误
        }
    }

    private void Update()
    {
        MoveDir = playerInput != null
                ? playerInput.PlayerControl.Move.ReadValue<Vector2>()
                : Vector2.zero;
        MoveDirRaw = MoveDir;
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
        playerInput.PlayerControl.SkillSpace.performed += OnSkillSpacePerformed;
        playerInput.UI.ESC.performed += OnESCPerformed;
    }

    private void HandleESCPressed()
    {
        // 优先级：从最高层级往低层级查找，只要有一个消费了 ESC 就停止
        // 1. 尝试关闭最高层级的 UI (Top -> Popup)
        if (UIManager.Instance.HandleBack(UILayer.Top)) return;
        if (UIManager.Instance.HandleBack(UILayer.Popup)) return;

        // 2. 尝试关闭 Normal 层 UI
        // 如果 HandleBack 返回 false，说明 Normal 层已经到主界面了，或者没东西关了
        if (!UIManager.Instance.HandleBack(UILayer.Normal))
        {
            // 3. 兜底逻辑：打开退出确认菜单（放在 Top 层）
            // 假设你的退出界面叫 QuitMenuView
            if (!UIManager.Instance.IsOpen<PauseUIView>())
            {
                _ = UIManager.Instance.Open<PauseUIView>(layer: UILayer.Top);
            }
        }
    }

    private void OnDisable()
    {
        playerInput?.Disable();
        if (playerInput != null)
        {
            playerInput.PlayerControl.SkillQ.performed -= OnSkillQPerformed;
            playerInput.PlayerControl.SkillE.performed -= OnSkillEPerformed;
            playerInput.PlayerControl.SkillSpace.performed -= OnSkillSpacePerformed;
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

    protected override void OnDestroy()
    {
        playerInput?.Dispose();
        base.OnDestroy();
    }

    private void OnSkillSpacePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        OnSkillSpacePressed?.Invoke();
    }
}
