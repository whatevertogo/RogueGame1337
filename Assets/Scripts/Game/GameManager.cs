using CDTU.Utils;
using RogueGame.Map;
using UI;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("References (optional, will auto-find if null)")]
    [SerializeField] private MonoBehaviour roomManagerTarget;
    public IRoomManager RoomManager => roomManagerTarget as IRoomManager;
    [SerializeField] private MonoBehaviour gameStateManagerTarget;
    public IGameStateManager GameStateManager => gameStateManagerTarget as IGameStateManager;

    [SerializeField] private UIManager UIManager;

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
        if (RoomManager == null)
        {
            CDTU.Utils.Logger.LogWarning("[GameManager] 房间管理器引用为空");
            return;
        }
        if (transitionController == null)
        {
            CDTU.Utils.Logger.LogWarning("[GameManager] TransitionController 引用为空，尝试在场景中查找。");
            return;
        }
        if (GameStateManager == null)
        {
            CDTU.Utils.Logger.LogWarning("[GameManager] GameStateManager 引用为空");
            return;

        }
        // 使用只读仓库与可写接口初始化 GameStateManager 的依赖
        GameStateManager.Initialize(RoomManager, RoomManager, transitionController, UIManager);
    }


    private void OnEnable()
    {
        // GameManager 不再直接处理门触发，流程由 GameStateManager 通过 EventBus 进行。
    }

    protected void Start()
    {
        // 通过 GameStateManager 启动 Run（GameStateManager 负责委托给 RoomManager）
        var startMeta = new RoomMeta { RoomType = RoomType.Start, Index = 0, BundleName = "Room_Start_0" };
        // 调试日志：帮助定位为何 StartRun 未被调用
        CDTU.Utils.Logger.Log($"[GameManager] Start: GameStateManager={(GameStateManager == null ? "null" : "present")}, RoomManager={(RoomManager == null ? "null" : "present")}, TransitionController={(transitionController == null ? "null" : "present")}");
        if (GameStateManager != null)
        {
            CDTU.Utils.Logger.Log("[GameManager] 调用 GameStateManager.StartRun(...)");
            GameStateManager.StartRun(startMeta);
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
        if (RoomManager == null) return false;
        return RoomManager.TryEnterDoor(dir);
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
