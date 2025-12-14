using UnityEngine;
using RogueGame.Map;
using CDTU.Utils;
using RogueGame.Events;
using System.Collections;

/// <summary>
/// 轻量级 GameStateManager：负责发布状态变更事件，保持最小实现以便快速验证流程。
/// 使用已有的 EventBus 进行解耦。用于替代/补充现有的 GameFlowController。
/// </summary>
public class GameStateManager : MonoBehaviour, IGameStateManager
{
    public GameFlowState CurrentState { get; private set; } = GameFlowState.None;

    // 当前层与房间上下文（最小）
    public int CurrentLayer { get; private set; } = 1;
    public int CurrentRoomId { get; private set; } = 0;
    // 可注入的 RoomManager 引用（由 GameManager 注入或自动查找）
    // 使用接口以降低耦合
    public IRoomManager RoomManager { get; set; }
    // 可注入的 TransitionController（用于执行过渡协程）
    public TransitionController TransitionController { get; set; }

    public void Initialize(IRoomManager roomManager, TransitionController transitionController)
    {
        this.RoomManager = roomManager;
        this.TransitionController = transitionController;
    }

    private bool _isTransitioning = false;
    private bool _subscribed = false;

    private void Start()
    {
    }

    /// <summary>
    /// 由上层调用以开始一次 Run（将委托 RoomManager 生成起始房）
    /// </summary>
    public void StartRun(RogueGame.Map.RoomMeta meta)
    {
        // 确保 RoomManager（优先使用注入的接口实现，否则查找场景中的具体实现并做接口转换）
        var rm = RoomManager ?? (RogueGame.Map.IRoomManager)FindObjectOfType<RogueGame.Map.RoomManager>();
        if (rm == null)
        {
            Debug.LogError("[GameStateManager] 找不到 RoomManager，无法开始 Run");
            return;
        }

        // 首先订阅事实事件，确保在 RoomManager 启动并发布事件时能被捕获。
        if (!_subscribed)
        {
            EventBus.Subscribe<DoorEnterRequestedEvent>(HandleDoorRequested);
            EventBus.Subscribe<RoomClearedEvent>(HandleRoomClearedEvent);
            EventBus.Subscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
            EventBus.Subscribe<CombatStartedEvent>(HandleCombatStartedEvent);
            _subscribed = true;
        }

        // 启动房间生成流程（RoomManager 内部会生成并发布 RoomEnteredEvent）
        rm.StartRun(meta);

        // 不再主动 EnterRoom：依赖低层发布的 RoomEnteredEvent 来驱动状态机以保持单一事实源。
        // 仍保留对 CurrentRoom 的一次性同步检查（若需要快速访问）
        CurrentRoomId = rm.CurrentRoom?.Meta?.Index ?? 0;
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
                EventBus.Publish(new ChooseNextRoomEvent { FromRoomId = CurrentRoomId });
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
        ChangeState(GameFlowState.EnterRoom);
        // 如果是战斗房则自动切换到战斗状态（最小实现）
        if (type == RoomType.Normal || type == RoomType.Elite || type == RoomType.Boss)
        {
            ChangeState(GameFlowState.RoomCombat);
        }
        else
        {
            // 非战斗房直接标记为已清理
            ChangeState(GameFlowState.RoomCleared);
        }
    }

    /// <summary>
    /// 房间内战斗结束，由 RoomManager/CombatManager 调用。
    /// </summary>
    public void OnRoomCombatEnded(int clearedEnemyCount)
    {
        // 低层 RoomController 将发布 RoomClearedEvent；这里只推进状态机为保险（可视需要保留或移除）
        ChangeState(GameFlowState.RoomCleared);
    }

    private void HandleRoomEnteredEvent(RoomEnteredEvent evt)
    {
        CurrentRoomId = evt.RoomId;
        EnterRoom(evt.RoomType, evt.RoomId);
    }

    private void HandleCombatStartedEvent(CombatStartedEvent evt)
    {
        CurrentRoomId = evt.RoomId;
        ChangeState(GameFlowState.RoomCombat);
    }

    private void HandleRoomClearedEvent(RoomClearedEvent evt)
    {
        // 本事件由 RoomManager 发布，GameStateManager 负责将流程推进到选择下一个房间或层间过渡
        // 更新内部状态
        CurrentRoomId = evt.RoomId;
        ChangeState(GameFlowState.RoomCleared);
    }

    private void HandleDoorRequested(DoorEnterRequestedEvent evt)
    {
        if (_isTransitioning) return;
        StartCoroutine(PerformRoomTransition(evt.Direction));
    }
    private IEnumerator PerformRoomTransition(Direction dir)
    {
        _isTransitioning = true;
        var tc = TransitionController ?? GameManager.Instance?.transitionController;

        if (tc != null)
        {
            // 在过渡过程中由 RoomManager 实际执行房间切换（通过接口调用）
            yield return StartCoroutine(tc.DoRoomTransitionCoroutine(() =>
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
    /// TODO:清理当前层级并加载新层级等复杂逻辑。
    /// </summary>
    public void TransitionToNextLayer()
    {
        // int from = CurrentLayer;
        // CurrentLayer++;
        // EventBus.Publish(new LayerTransitionEvent { FromLayer = from, ToLayer = CurrentLayer });
        // // 进入新的起始房
        // EnterRoom(RoomType.Start, 0);
    }
}
