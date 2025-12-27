using CDTU.Utils;
using RogueGame.GameConfig;
using RogueGame.Game.Service.SkillLimit;
using RogueGame.Map;
using RogueGame.SaveSystem;
using UI;
using UnityEngine;

/// <summary>
/// 游戏根节点，管理全局单例和核心系统
/// </summary>
public class GameRoot : Singleton<GameRoot>
{
    [Header("DebugSettings")]
    [Tooltip("启用全局调试日志输出")]
    public bool enableDebugLogs = false;


    [Header("Game DataBases")]
    [SerializeField] private CardDataBase cardDatabase;
    public CardDataBase CardDatabase => cardDatabase;

    [Header("Game Configs")]
    //游戏胜利奖励配置
    [Tooltip("层间胜利奖励配置")]
    [SerializeField] private GameWinLayerRewardConfig gameWinLayerRewardConfig;
    public GameWinLayerRewardConfig GameWinLayerRewardConfig => gameWinLayerRewardConfig;

    //主动卡去重配置
    [Tooltip("重复主动卡转换为金币的配置")]
    [SerializeField] private ActiveCardDeduplicationConfig activeCardDeduplicationConfig;
    public ActiveCardDeduplicationConfig ActiveCardDeduplicationConfig => activeCardDeduplicationConfig;

    //难度曲线配置
    [Tooltip("游戏难度曲线配置")]
    [SerializeField] private DifficultyCurveConfig difficultyCurveConfig;
    public DifficultyCurveConfig DifficultyCurveConfig => difficultyCurveConfig;

    //属性上限配置
    [Tooltip("属性上限配置（防止数值爆炸）")]
    [SerializeField] private StatLimitConfig statLimitConfig;
    public StatLimitConfig StatLimitConfig => statLimitConfig;

    [Header("Scene Managers")]
    [SerializeField] private GameFlowCoordinator gameFlowCoordinator;
    public GameFlowCoordinator GameFlowCoordinator => gameFlowCoordinator;
    [SerializeField] private RoomManager roomManager;
    public RoomManager RoomManager => roomManager;
    [SerializeField] private UIManager uiManager;
    public UIManager UIManager => uiManager;
    [SerializeField] private TransitionController transitionController;
    public TransitionController TransitionController => transitionController;
    [SerializeField] private PlayerManager playerManager;
    public PlayerManager PlayerManager => playerManager;
    [SerializeField] private InventoryServiceManager inventoryManager;

    public InventoryServiceManager InventoryManager => inventoryManager;
    [SerializeField] private LootDropper lootDropper;
    public LootDropper LootDropper => lootDropper;
    [SerializeField] private SaveManager saveManager;
    public SaveManager SaveManager => saveManager;

    [SerializeField] private ShopManager shopManager;
    public ShopManager ShopManager => shopManager;

    [SerializeField] private GameInput gameInput;

    public GameInput GameInput => gameInput;

    // Services
    // SlotService放GameRoot了
    // public SlotService SlotService => GetComponent<SlotService>();


    //战斗奖励技能服务
    public SkillChargeSyncService SkillChargeSyncService { get; private set; }

    //层间奖励系统服务
    public FloorRewardSystemService FloorRewardSystemService { get; private set; }

    //难度系统服务
    public DifficultyService DifficultyService { get; private set; }

    //被动卡牌效果应用服务
    public PassiveCardApplicationService PassiveCardApplicationService { get; private set; }

    //充能服务
    public CombatRewardEnergyService CombatRewardEnergyService { get; private set; }

    //RoomPlayerSkillLimitService房间技能限制服务
    public RoomPlayerSkillLimitService RoomPlayerSkillLimitService { get; private set; }


    protected override void Awake()
    {
        base.Awake();
        CDLogger.Log("[GameRoot] Awake()");

        bool ok = true;

        ok &= AssertNotNull(cardDatabase, nameof(cardDatabase));
        ok &= AssertNotNull(gameFlowCoordinator, nameof(gameFlowCoordinator));
        ok &= AssertNotNull(roomManager, nameof(roomManager));
        ok &= AssertNotNull(uiManager, nameof(uiManager));
        ok &= AssertNotNull(transitionController, nameof(transitionController));
        ok &= AssertNotNull(playerManager, nameof(playerManager));
        ok &= AssertNotNull(inventoryManager, nameof(inventoryManager));
        ok &= AssertNotNull(lootDropper, nameof(lootDropper));
        ok &= AssertNotNull(saveManager, nameof(saveManager));
        ok &= AssertNotNull(shopManager, nameof(shopManager));
        ok &= AssertNotNull(gameWinLayerRewardConfig, nameof(gameWinLayerRewardConfig));
        ok &= AssertNotNull(activeCardDeduplicationConfig, nameof(activeCardDeduplicationConfig));
        ok &= AssertNotNull(difficultyCurveConfig, nameof(difficultyCurveConfig));

        // statLimitConfig 是可选的，如果未配置则使用默认值
        if (statLimitConfig == null)
        {
            CDLogger.LogError("[GameRoot] StatLimitConfig 未配置, 使用默认值");
            statLimitConfig = ScriptableObject.CreateInstance<StatLimitConfig>();
        }

        if (!ok)
        {
            CDLogger.LogError("[GameRoot] Initialization aborted due to missing references.");
            return;
        }

        // 确保 SlotService 在运行时存在，用于处理槽位相关集中逻辑（例如响应 ClearAllSlotsRequestedEvent）
        if (GetComponent<SlotService>() == null)
        {
            gameObject.AddComponent<SlotService>();
            CDLogger.Log("[GameRoot] SlotService added to GameRoot at runtime");
        }

        CDLogger.Log("[GameRoot] All required references assigned. Initializing CardDatabase.");
        cardDatabase.Initialize();
        //SaveManager 初始化依赖于 PlayerManager 和 InventoryManager
        //TODO:项目最后来写保存游戏功能SaveManager

        gameFlowCoordinator.Initialize(
            roomManager,
            transitionController,
            uiManager
        );

        transitionController.Initialize(playerManager);

        roomManager.Initialize(transitionController);

        playerManager.Initialize(roomManager);

        shopManager.Initialize(inventoryManager);

        SkillChargeSyncService = new SkillChargeSyncService(
            inventoryManager,
            playerManager,
            cardDatabase
        );

        FloorRewardSystemService = new FloorRewardSystemService(
            playerManager,
            inventoryManager,
            gameWinLayerRewardConfig
        );

        DifficultyService = new DifficultyService(difficultyCurveConfig);

        PassiveCardApplicationService = new PassiveCardApplicationService(
            inventoryManager,
            playerManager,
            cardDatabase
        );

        RoomPlayerSkillLimitService = new RoomPlayerSkillLimitService(
            playerManager,
            roomManager,
            inventoryManager
        );

        CombatRewardEnergyService = new CombatRewardEnergyService(inventoryManager);

        // 订阅服务的事件
        FloorRewardSystemService.Subscribe();
        PassiveCardApplicationService.Subscribe();
        RoomPlayerSkillLimitService.Subscribe();

        // 启动时加载元游戏存档
        SaveManager.LoadMeta();
    }

    private bool AssertNotNull(Object obj, string name)
    {
        if (obj == null)
        {
            CDLogger.LogError($"[GameRoot] {name} is not assigned.");
            return false;
        }
        return true;
    }


    protected override void OnDestroy()
    {
        CDLogger.Log("[GameRoot] OnDestroy() called.");

        // 取消订阅服务的事件
        FloorRewardSystemService?.Unsubscribe();
        PassiveCardApplicationService?.Unsubscribe();
        RoomPlayerSkillLimitService?.Unsubscribe();

        base.OnDestroy();
    }
}
