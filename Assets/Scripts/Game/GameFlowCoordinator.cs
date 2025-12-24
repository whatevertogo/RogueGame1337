using UnityEngine;
using RogueGame.Map;
using RogueGame.Events;
using System.Collections;
using UI;
using RogueGame.SaveSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 轻量级 GameStateManager：负责发布状态变更事件，保持最小实现以便快速验证流程。
/// 使用已有的 EventBus 进行解耦。用于替代/补充现有的 GameFlowController。
/// </summary>
public sealed class GameFlowCoordinator : MonoBehaviour, IGameStateManager
{
    public GameFlowState CurrentState { get; private set; } = GameFlowState.None;

    // 当前层与房间上下文
    public int CurrentLayer { get; private set; } = 1;
    public int CurrentRoomId { get; private set; } = 0;
    public int CurrentRoomInstanceId { get; private set; } = 0;
    // 可注入的只读房间仓库与可写房间管理接口（由 GameManager 注入）
    // 使用 IReadOnlyRoomRepository 进行查询以保证只读契约，使用 IRoomManager 执行需写入的操作
    public IReadOnlyRoomRepository RoomRepository { get; private set; }
    public IRoomManager RoomManager { get; private set; }
    // 可注入的 TransitionController（用于执行过渡协程）
    public TransitionController TransitionController { get; set; }
    public UIManager UIManager { get; private set; }

    public void Initialize(RoomManager roomManager, TransitionController transitionController, UIManager uiManager)
    {
        this.RoomManager = roomManager;
        // 注入只读仓库接口，确保 StartRun 有可用的只读数据源
        this.RoomRepository = roomManager;
        this.TransitionController = transitionController;
        this.UIManager = uiManager;
        if (roomManager == null)
        {
            CDTU.Utils.CDLogger.LogWarning("[GameFlowCoordinator] Initialize called with null RoomManager");
        }
    }

    private bool _isTransitioning = false;
    private bool _subscribed = false;
    // 每层已清理的战斗房间数（GameStateManager 为权威）
    private int _roomsClearedThisLayer = 0;
    private bool _bossUnlockedThisLayer = false;

    private bool _restoredRunHandled = false;

    private void Start()
    {
        // // 订阅 Run 存档加载事件，以便在存在上次 Run 存档时恢复并启动该 Run
        // SaveManager.OnRunSaveLoaded += HandleRunSaveLoaded;

        // // 如果 SaveManager 已经在其它地方（例如 SaveManager.Start）加载了 Run 存档，立即处理它
        // var saveManager = GameRoot.Instance?.SaveManager;
        // if (saveManager != null && saveManager.CurrentRunSave != null)
        // {
        //     HandleRunSaveLoaded(saveManager.CurrentRunSave);
        //     return;
        // }

        // 否则按默认流程开启新的 Run
        var startMeta = new RoomMeta { RoomType = RoomType.Start, Index = 0, BundleName = "Room_Start_0" };
        StartRun(startMeta);
    }

    /// <summary>
    /// 由上层调用以开始一次 Run（将委托 RoomManager 生成起始房）
    /// </summary>
    public void StartRun(RoomMeta meta)
    {
        if (RoomManager == null || RoomRepository == null)
        {
            CDTU.Utils.CDLogger.LogError("GameFlowCoordinator: RoomManager is not set. Cannot start run.");
            return;
        }

        // 订阅事实事件，确保在 RoomManager 启动并发布事件时能被捕获。
        if (!_subscribed)
        {
            EventBus.Subscribe<DoorEnterRequestedEvent>(HandleDoorRequested);
            EventBus.Subscribe<RoomClearedEvent>(HandleRoomClearedEvent);
            EventBus.Subscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
            EventBus.Subscribe<CombatStartedEvent>(HandleCombatStartedEvent);
            _subscribed = true;
        }

        // 将层级初始化交由 GameFlowCoordinator 处理：设置 CurrentLayer 并委托 RoomManager 启动该层
        CurrentLayer = 1;
        _roomsClearedThisLayer = 0;
        _bossUnlockedThisLayer = false;
        RoomManager.StartFloor(CurrentLayer, meta);
        CDTU.Utils.CDLogger.Log("GameFlowCoordinator: StartRun called for Layer " + CurrentLayer);

        // 初始化UI，由单独的ui入口使用
        // UIManager?.Open<PlayingStateUIView>(layer: UILayer.Normal);

        // 不再主动 EnterRoom：依赖低层发布的 RoomEnteredEvent 来驱动状态机以保持单一事实源。
        // 仍保留对 CurrentRoom 的一次性同步检查（通过只读仓库查询最新实例）
        var latest = GetLatestInstanceFromRepository();
        if (latest != null)
        {
            CurrentRoomId = latest.Meta?.Index ?? 0;
            CurrentRoomInstanceId = latest.InstanceId;
        }
    }

    private void OnDisable()
    {
        if (_subscribed)
        {
            EventBus.Unsubscribe<DoorEnterRequestedEvent>(HandleDoorRequested);
            EventBus.Unsubscribe<RoomClearedEvent>(HandleRoomClearedEvent);
            EventBus.Unsubscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
            EventBus.Unsubscribe<CombatStartedEvent>(HandleCombatStartedEvent);
            _subscribed = false;
        }

        // 退订 Run 存档加载事件
        try { SaveManager.OnRunSaveLoaded -= HandleRunSaveLoaded; } catch { }
    }

    private RoomInstanceState GetLatestInstanceFromRepository()
    {
        if (RoomRepository == null) return null;
        RoomInstanceState latest = null;
        foreach (var inst in RoomRepository.GetAllInstances())
        {
            if (inst == null) continue;
            if (latest == null || inst.InstanceId > latest.InstanceId)
            {
                latest = inst;
            }
        }
        return latest;
    }

    private void HandleRunSaveLoaded(RunSaveData data)
    {
        if (data == null || _restoredRunHandled) return;
        _restoredRunHandled = true;

        // 应用最小恢复行为：设置层级并启动该层（详尽的房间恢复留给 SaveRestoreUtility.RestoreGameState）
        CurrentLayer = Mathf.Max(1, data.CurrentLayer);
        _roomsClearedThisLayer = 0;
        _bossUnlockedThisLayer = false;

        var startMeta = new RoomMeta { RoomType = RoomType.Start, Index = 0, BundleName = "Room_Start_0" };
        RoomManager?.StartFloor(CurrentLayer, startMeta);

        CDTU.Utils.CDLogger.Log($"GameFlowCoordinator: Restored Run to Layer {CurrentLayer} from save.");
    }

    public void ChangeState(GameFlowState newState)
    {
        if (newState == CurrentState) return;
        ExitState(CurrentState);
        CurrentState = newState;
        EnterState(CurrentState);
    }

    private void EnterState(GameFlowState state)
    {
        switch (state)
        {
            case GameFlowState.EnterRoom:
                // 事实事件（RoomEntered/CombatStarted/RoomCleared）由低层 RoomController/RoomManager 发布，
                break;
            case GameFlowState.RoomCombat:
                // 低层负责发布 CombatStartedEvent
                break;
            case GameFlowState.RoomCleared:
                // 低层负责发布 RoomClearedEvent
                break;
            case GameFlowState.ChooseNextRoom:
                EventBus.Publish(new ChooseNextRoomEvent { FromRoomId = CurrentRoomId, FromRoomInstanceId = CurrentRoomInstanceId });
                break;
            default:
                break;
        }
    }

    private void ExitState(GameFlowState state)
    {
        // 目前无需特殊退出逻辑；保留扩展点
    }

    /// <summary>
    /// 进入指定房间（由 RoomManager 等调用）。
    /// </summary>
    public void EnterRoom(RoomType type, int roomId)
    {
        CurrentRoomId = roomId;
        // 触发 EnterRoom 状态
        // 仅进入 EnterRoom 状态；是否进入战斗应由 RoomController 发布的事实（CombatStartedEvent / RoomClearedEvent）驱动
        // 实际进入的房间类型可由 RoomManager控制
        ChangeState(GameFlowState.EnterRoom);
    }

    // NOTE: RoomController 发布 RoomClearedEvent/CombatStartedEvent as facts.
    // GameFlowCoordinator 不再依赖外部直接调用以推进流程（例如 OnRoomCombatEnded），
    // 一切流程推进应由订阅事实事件执行，保留此处为空以避免双向调用。

    private void HandleRoomEnteredEvent(RoomEnteredEvent evt)
    {
        CurrentRoomId = evt.RoomId;
        CurrentRoomInstanceId = evt.InstanceId;
        EnterRoom(evt.RoomType, evt.RoomId);
    }

    private void HandleCombatStartedEvent(CombatStartedEvent evt)
    {
        CurrentRoomId = evt.RoomId;
        CurrentRoomInstanceId = evt.InstanceId;
        ChangeState(GameFlowState.RoomCombat);
    }

    private void HandleRoomClearedEvent(RoomClearedEvent evt)
    {
        // 本事件由 RoomManager 发布，GameStateManager 负责将流程推进到选择下一个房间或层间过渡
        // 更新内部状态
        CurrentRoomId = evt.RoomId;
        CurrentRoomInstanceId = evt.InstanceId;
        // 进入已清理状态
        ChangeState(GameFlowState.RoomCleared);

        // ✅ 房间清理后解锁玩家移动
        TransitionController?.UnlockAllPlayersMovement();
        CDTU.Utils.CDLogger.Log($"[GameFlowCoordinator] 房间清理完成，已解锁玩家移动");

        // 统计本层已清理战斗房间数（仅对战斗房间计数；RoomClearedEvent 的 RoomType 可用于判断）
        if (evt.RoomType == RoomType.Normal || evt.RoomType == RoomType.Elite)
        {
            _roomsClearedThisLayer++;
            // 检查 Boss 解锁阈值
            int threshold = 0;
            try { threshold = RoomManager.GetBossUnlockThreshold(); } catch { threshold = 0; }
            // 1.本层未解锁Boss,2.阈值大于0,3.已清理房间数达到阈值
            if (!_bossUnlockedThisLayer && threshold > 0 && _roomsClearedThisLayer >= threshold)
            {
                _bossUnlockedThisLayer = true;
                EventBus.Publish(new BossUnlockedEvent { Layer = CurrentLayer });
            }
        }

        // 若清理的是 Boss 房，则触发层间过渡
        if (evt.RoomType == RoomType.Boss)
        {
            TransitionToNextLayer();
            return;
        }

        // 进入选择下一个房间阶段
        ChangeState(GameFlowState.ChooseNextRoom);
    }

    private void HandleDoorRequested(DoorEnterRequestedEvent evt)
    {
        if (_isTransitioning) return;
        StartCoroutine(PerformRoomTransition(evt.Direction));
    }

    private IEnumerator PerformRoomTransition(Direction dir)
    {
        _isTransitioning = true;
        if (TransitionController != null)
        {
            // 在过渡过程中由 RoomManager 实际执行房间切换（通过接口调用）
            yield return StartCoroutine(TransitionController.DoRoomTransitionCoroutine(() =>
            {
                RoomManager?.SwitchToNextRoom(dir);
            }));
        }
        else
        {
            RoomManager?.SwitchToNextRoom(dir);
        }
        _isTransitioning = false;
    }

    /// <summary>
    /// 进入选择下一个房间阶段（由 RoomManager 调用）。
    /// </summary>
    public void ChooseNextRoom()
    {
        ChangeState(GameFlowState.ChooseNextRoom);
    }

    /// <summary>
    /// 触发层间过渡（击败 Boss 后）。
    /// 发放层间奖励（满血 + 40 金币 + 随机卡牌）并启动新层级
    /// </summary>
    public void TransitionToNextLayer()
    {
        int from = CurrentLayer;
        CurrentLayer++;
        _roomsClearedThisLayer = 0;
        _bossUnlockedThisLayer = false;

        try
        {
            // 发布层间过渡事件
            EventBus.Publish(new LayerTransitionEvent(from,CurrentLayer));

            // 触发层间奖励系统
            EventBus.Publish(new RewardSelectionRequestedEvent(
                roomId: 0,
                instanceId: 0,
                roomType: RoomType.Start,
                currentLayer: CurrentLayer
            ));
        }
        catch { }

        // 启动新层级
        var startMeta = new RoomMeta { RoomType = RoomType.Start, Index = 0, BundleName = "Room_Start_0" };
        RoomManager?.StartFloor(CurrentLayer, startMeta);
    }

    #region 暂停与恢复游戏
    public void PauseGame()
    {
        Time.timeScale = 0f;
        // 额外逻辑：通知 UI/Audio
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        // 1. 📊 保存本局统计到元游戏存档
        var saveManager = GameRoot.Instance?.SaveManager;
        if (saveManager != null)
        {
            saveManager.SaveRunToMetaOnDeath();  // 保存当前单局数据到元游戏存档
            saveManager.ClearRunSave();          // 清空单局存档
        }

        //2. 重置时间缩放
        Time.timeScale = 1f;

        // 3. 重置状态(Scene自己会重置)
        // CurrentState = GameFlowState.None;
        // CurrentLayer = 1;
        // CurrentRoomId = 0;
        // CurrentRoomInstanceId = 0;
        // _roomsClearedThisLayer = 0;
        // _bossUnlockedThisLayer = false;


        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    public void EndGame()
    {
        // 结束游戏逻辑：清理状态，显示结算界面等
        Time.timeScale = 1f;
        CurrentState = GameFlowState.None;
        CurrentLayer = 1;
        CurrentRoomId = 0;
        CurrentRoomInstanceId = 0;
        _roomsClearedThisLayer = 0;
        _bossUnlockedThisLayer = false;

        // 额外逻辑：通知 UI/Audio，显示结算界面等
    }
    #endregion
}
