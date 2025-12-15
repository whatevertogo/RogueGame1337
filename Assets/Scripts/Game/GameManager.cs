using System;
using System.Collections;
using CDTU.Utils;
using RogueGame.Map;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("References (optional, will auto-find if null)")]
    [SerializeField] private MonoBehaviour roomManagerTarget;
    public IRoomManager roomManager => roomManagerTarget as IRoomManager;
    [SerializeField] private MonoBehaviour gameStateManagerTarget;
    public IGameStateManager gameStateManager => gameStateManagerTarget as IGameStateManager;

    public TransitionController transitionController;

    protected override void Awake()
    {
        base.Awake();
        CheckReferences();
    }

    /// <summary>
    /// 检查并自动填充必要引用
    /// </summary>
    private void CheckReferences()
    {
        if (roomManager == null)
        {
            CDTU.Utils.Logger.LogWarning("[GameManager] 房间管理器引用为空");
        }
        if (transitionController == null)
        {
            CDTU.Utils.Logger.LogWarning("[GameManager] TransitionController 引用为空，尝试在场景中查找。");
            transitionController = FindObjectOfType<TransitionController>();
        }
        if (gameStateManager == null)
        {
            CDTU.Utils.Logger.LogWarning("[GameManager] GameStateManager 引用为空");
        }

        // 将 RoomManager 注入到 GameStateManager，确保 GameStateManager 能够委托 RoomManager 启动 Run
        if (gameStateManager != null && roomManager != null)
        {
            // 使用只读仓库与可写接口初始化 GameStateManager 的依赖
            gameStateManager.Initialize(roomManager, roomManager, transitionController);
        }

        // 若场景中仍无 TransitionController，则自动创建并初始化（保证 RoomManager 能够依赖它）
        if (transitionController == null)
        {
            var go = new GameObject("TransitionController");
            transitionController = go.AddComponent<TransitionController>();
            // 尝试设置主相机引用
            if (transitionController.mainCamera == null && Camera.main != null)
            {
                transitionController.mainCamera = Camera.main;
            }
            // 设置合理默认值（若未在 Inspector 调整）
            if (transitionController.cameraSwitchDuration <= 0f) transitionController.cameraSwitchDuration = 0.5f;
            if (transitionController.teleportMovementDisableTime <= 0f) transitionController.teleportMovementDisableTime = 0.15f;
            Debug.Log("[GameManager] 自动创建 TransitionController 并注入到 GameManager");
        }
        else
        {
            // 如果找到 TransitionController，确保其 mainCamera 被设置（降低配置错误概率）
            if (transitionController.mainCamera == null && Camera.main != null)
            {
                transitionController.mainCamera = Camera.main;
            }
        }

        // 确保如果刚刚创建或找到 TransitionController，注入到 GameStateManager
        if (gameStateManager != null)
        {
            // 通过 Initialize 保证 GameStateManager 收到最新注入（roomManager 可为空，Initialize 会处理）
            gameStateManager.Initialize(roomManager, roomManager, transitionController);
        }
    }

    private void OnEnable()
    {
        // GameManager 不再直接处理门触发，流程由 GameStateManager 通过 EventBus 进行。
    }

    protected void Start()
    {
        // 通过 GameStateManager 启动 Run（GameStateManager 负责委托给 RoomManager）
        var startMeta = new RoomMeta { RoomType = RoomType.Start, Index = 0, BundleName = "Room_Start_0" };
        if (gameStateManager != null)
        {
            gameStateManager.StartRun(startMeta);
        }
        else
        {
            Debug.LogError("[GameManager] 找不到 GameStateManager，无法启动 Run。请检查引用或场景初始化。");
        }
    }

    private void OnDisable()
    {
        // nothing
    }

    /// <summary>
    /// 外部调用尝试进入门（UI/输入使用）。内部会委托给 RoomManager 验证并触发 OnDoorEnterRequested 事件。
    /// </summary>
    public bool TryEnterDoor(Direction dir)
    {
        if (roomManager == null) return false;
        return roomManager.TryEnterDoor(dir);
    }

    // Door transition orchestration moved to GameStateManager

    #region Pause / Resume helpers
    public void PauseGame()
    {
        Time.timeScale = 0f;
        // 额外逻辑：通知 UI/Audio
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    #endregion
}