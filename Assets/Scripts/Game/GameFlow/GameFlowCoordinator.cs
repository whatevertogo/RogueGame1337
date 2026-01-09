using CDTU.Utils;
using Core.Events;
using RogueGame.Events;
using RogueGame.Map;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏流程协调器：负责房间切换、状态流转、层级管理
/// 核心职责：
/// 1. 订阅底层事件（RoomEntered/CombatStarted/RoomCleared）驱动状态机
/// 2. 管理层级进度（已清理房间数、Boss解锁状态）
/// 3. 协调房间过渡（通过 TransitionController）
/// 4. 发布流程事件（ChooseNextRoom/BossUnlocked/LayerTransition）
/// </summary>
public sealed class GameFlowCoordinator : MonoBehaviour, IGameFlowCoordinator
{
    // ═══════════════════════════════════════════════════════════
    // 公开状态
    // ═══════════════════════════════════════════════════════════

    public GameFlowState CurrentState { get; private set; } = GameFlowState.None;

    /// <summary>
    /// 当前层级（从 1 开始）
    /// </summary>
    public int CurrentLayer { get; private set; } = 1;

    /// <summary>
    /// 当前房间 ID
    /// </summary>
    public int CurrentRoomId { get; private set; }

    /// <summary>
    /// 当前房间实例 ID
    /// </summary>
    public int CurrentRoomInstanceId { get; private set; }

    // ═══════════════════════════════════════════════════════════
    // 依赖注入（由 GameRoot 在启动时注入）
    // ═══════════════════════════════════════════════════════════

    public IReadOnlyRoomRepository RoomRepository { get; private set; }
    public IRoomManager RoomManager { get; private set; }
    public TransitionController TransitionController { get; set; }
    public UIManager UIManager { get; private set; }

    // ═══════════════════════════════════════════════════════════
    // 私有状态
    // ═══════════════════════════════════════════════════════════

    private bool _isTransitioning = false;
    private int _roomsClearedThisLayer = 0;
    private bool _bossUnlockedThisLayer = false;

    // ═══════════════════════════════════════════════════════════
    // 初始化
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 初始化流程协调器（由 GameRoot 调用）
    /// </summary>
    public void Initialize(
        RoomManager roomManager,
        TransitionController transitionController,
        UIManager uiManager
    )
    {
        RoomManager = roomManager;
        RoomRepository = roomManager;
        TransitionController = transitionController;
        UIManager = uiManager;

        if (roomManager == null)
        {
            CDLogger.LogWarning("[GameFlowCoordinator] RoomManager 为 null，无法启动游戏");
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DoorEnterRequestedEvent>(HandleDoorRequested);
        EventBus.Subscribe<RoomClearedEvent>(HandleRoomCleared);
        EventBus.Subscribe<RoomEnteredEvent>(HandleRoomEntered);
        EventBus.Subscribe<CombatStartedEvent>(HandleCombatStarted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DoorEnterRequestedEvent>(HandleDoorRequested);
        EventBus.Unsubscribe<RoomClearedEvent>(HandleRoomCleared);
        EventBus.Unsubscribe<RoomEnteredEvent>(HandleRoomEntered);
        EventBus.Unsubscribe<CombatStartedEvent>(HandleCombatStarted);
    }

    private void Start()
    {
        var startMeta = new RoomMeta
        {
            RoomType = RoomType.Start,
            Index = 0,
            BundleName = "Room_Start_0",
        };
        StartRun(startMeta);
    }

    // ═══════════════════════════════════════════════════════════
    // Run 管理
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 开始新的 Run
    /// </summary>
    public void StartRun(RoomMeta meta)
    {
        if (RoomManager == null)
        {
            CDLogger.LogError("[GameFlowCoordinator] RoomManager 未初始化，无法开始 Run");
            return;
        }

        CurrentLayer = 1;
        _roomsClearedThisLayer = 0;
        _bossUnlockedThisLayer = false;

        RoomManager.StartFloor(CurrentLayer, meta);
        CDLogger.Log($"[GameFlowCoordinator] 启动 Run，层级 {CurrentLayer}");

        // 同步当前房间信息
        var latest = GetLatestRoomInstance();
        if (latest != null)
        {
            CurrentRoomId = latest.Meta.Index;
            CurrentRoomInstanceId = latest.InstanceId;
        }
    }

    /// <summary>
    /// 从仓库获取最新的房间实例
    /// </summary>
    private RoomInstanceState GetLatestRoomInstance()
    {
        if (RoomRepository == null)
            return null;

        RoomInstanceState latest = null;
        foreach (var inst in RoomRepository.GetAllInstances())
        {
            if (latest == null || inst.InstanceId > latest.InstanceId)
            {
                latest = inst;
            }
        }
        return latest;
    }

    // ═══════════════════════════════════════════════════════════
    // 状态机
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 切换游戏流程状态
    /// </summary>
    public void ChangeState(GameFlowState newState)
    {
        if (newState == CurrentState)
            return;

        CurrentState = newState;
        OnStateChanged(newState);
    }

    /// <summary>
    /// 状态变化时的处理
    /// </summary>
    private void OnStateChanged(GameFlowState state)
    {
        switch (state)
        {
            case GameFlowState.ChooseNextRoom:
                EventBus.Publish(
                    new ChooseNextRoomEvent
                    {
                        FromRoomId = CurrentRoomId,
                        FromRoomInstanceId = CurrentRoomInstanceId,
                    }
                );
                break;

            // 以下状态由底层事件驱动，无需额外处理
            case GameFlowState.EnterRoom:
            case GameFlowState.RoomCombat:
            case GameFlowState.RoomCleared:
            default:
                break;
        }
    }

    /// <summary>
    /// 进入房间（由 RoomManager 调用）
    /// </summary>
    public void EnterRoom(RoomType type, int roomId)
    {
        CurrentRoomId = roomId;
        ChangeState(GameFlowState.EnterRoom);
    }

    // ═══════════════════════════════════════════════════════════
    // 事件处理
    // ═══════════════════════════════════════════════════════════

    private void HandleRoomEntered(RoomEnteredEvent evt)
    {
        CurrentRoomId = evt.RoomId;
        CurrentRoomInstanceId = evt.InstanceId;
        EnterRoom(evt.RoomType, evt.RoomId);
    }

    private void HandleCombatStarted(CombatStartedEvent evt)
    {
        CurrentRoomId = evt.RoomId;
        CurrentRoomInstanceId = evt.InstanceId;
        ChangeState(GameFlowState.RoomCombat);
    }

    private void HandleRoomCleared(RoomClearedEvent evt)
    {
        CurrentRoomId = evt.RoomId;
        CurrentRoomInstanceId = evt.InstanceId;
        ChangeState(GameFlowState.RoomCleared);

        // 统计战斗房间清理数
        if (evt.RoomType == RoomType.Normal || evt.RoomType == RoomType.Elite)
        {
            _roomsClearedThisLayer++;
            TryUnlockBoss();
        }

        // Boss 房间触发层级过渡
        if (evt.RoomType == RoomType.Boss)
        {
            TransitionToNextLayer();
            return;
        }

        ChangeState(GameFlowState.ChooseNextRoom);
    }

    private void HandleDoorRequested(DoorEnterRequestedEvent evt)
    {
        if (_isTransitioning)
            return;

        StartCoroutine(PerformRoomTransition(evt.Direction));
    }

    // ═══════════════════════════════════════════════════════════
    // 层级管理
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 尝试解锁 Boss 房间
    /// </summary>
    private void TryUnlockBoss()
    {
        if (_bossUnlockedThisLayer)
            return;

        int threshold = RoomManager.GetBossUnlockThreshold();

        if (threshold > 0 && _roomsClearedThisLayer >= threshold)
        {
            _bossUnlockedThisLayer = true;
            EventBus.Publish(new BossUnlockedEvent { Layer = CurrentLayer });
            CDLogger.Log($"[GameFlowCoordinator] Boss 房间已解锁，层级 {CurrentLayer}");
        }
    }

    /// <summary>
    /// 过渡到下一层级（击败 Boss 后）
    /// </summary>
    public void TransitionToNextLayer()
    {
        int fromLayer = CurrentLayer;
        CurrentLayer++;

        // 重置层级状态
        _roomsClearedThisLayer = 0;
        _bossUnlockedThisLayer = false;

        // 发布层级过渡事件
        EventBus.Publish(new LayerTransitionEvent(fromLayer, CurrentLayer));

        // 发放层间奖励
        EventBus.Publish(
            new RewardSelectionRequestedEvent(
                roomId: 0,
                instanceId: 0,
                roomType: RoomType.Start,
                currentLayer: CurrentLayer
            )
        );

        // 启动新层级
        var startMeta = new RoomMeta
        {
            RoomType = RoomType.Start,
            Index = 0,
            BundleName = "Room_Start_0",
        };
        RoomManager?.StartFloor(CurrentLayer, startMeta);
    }

    // ═══════════════════════════════════════════════════════════
    // 房间过渡
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 执行房间切换协程
    /// </summary>
    private System.Collections.IEnumerator PerformRoomTransition(Direction dir)
    {
        _isTransitioning = true;

        if (TransitionController != null)
        {
            yield return StartCoroutine(
                TransitionController.DoRoomTransitionCoroutine(() =>
                {
                    RoomManager?.SwitchToNextRoom(dir);
                })
            );
        }
        else
        {
            RoomManager?.SwitchToNextRoom(dir);
        }

        _isTransitioning = false;
    }

    // ═══════════════════════════════════════════════════════════
    // 游戏控制
    // ═══════════════════════════════════════════════════════════

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EndGame()
    {
        Time.timeScale = 1f;
        CurrentState = GameFlowState.None;
        CurrentLayer = 1;
        CurrentRoomId = 0;
        CurrentRoomInstanceId = 0;
        _roomsClearedThisLayer = 0;
        _bossUnlockedThisLayer = false;
    }
}
