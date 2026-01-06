using CDTU.Utils;
using Character.Player.Skill.Evolution;
using RogueGame.GameConfig;
using RogueGame.Game.Service.SkillLimit;
using RogueGame.Map;
using UI;
using UnityEngine;
using RogueGame.Game.Service;

/// <summary>
/// 游戏根节点，管理全局单例和核心系统
///
/// 职责：
/// 1. 初始化所有核心管理器和服务
/// 2. 将服务注册到 ServiceLocator 供全局访问
/// 3. 管理服务生命周期（事件订阅/取消订阅）
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

    [Header("Evolution System")]
    [Tooltip("技能进化效果池（全局单例）")]
    [SerializeField] private EvolutionEffectPool evolutionEffectPool;
    public EvolutionEffectPool EvolutionEffectPool => evolutionEffectPool;

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
    InventoryServiceManager inventoryManager = new();

    public InventoryServiceManager InventoryManager => inventoryManager;
    [SerializeField] private LootDropper lootDropper;
    public LootDropper LootDropper => lootDropper;
    // [SerializeField] private SaveManager saveManager;
    // public SaveManager SaveManager => saveManager;

    [SerializeField] private ShopManager shopManager;
    public ShopManager ShopManager => shopManager;

    [SerializeField] private GameInput gameInput;

    public GameInput GameInput => gameInput;

    // -------------Services----------------


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

    //slotService 放在 GameRoot 上，确保运行时存在
    public SlotClearService SlotService { get; private set; }



    protected override void Awake()
    {
        base.Awake();
        CDLogger.Log("[GameRoot] Awake() - 开始初始化");

        bool ok = true;

        ok &= AssertNotNull(cardDatabase, nameof(cardDatabase));
        ok &= AssertNotNull(gameFlowCoordinator, nameof(gameFlowCoordinator));
        ok &= AssertNotNull(roomManager, nameof(roomManager));
        ok &= AssertNotNull(uiManager, nameof(uiManager));
        ok &= AssertNotNull(transitionController, nameof(transitionController));
        ok &= AssertNotNull(playerManager, nameof(playerManager));
        ok &= AssertNotNull(lootDropper, nameof(lootDropper));
        // ok &= AssertNotNull(saveManager, nameof(saveManager));
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


        CDLogger.Log("[GameRoot] All required references assigned. Initializing CardDatabase.");
        cardDatabase.Initialize();
        //SaveManager 初始化依赖于 PlayerManager 和 InventoryManager
        //TODO:项目最后来写保存游戏功能SaveManager

        // ========== 初始化管理器 ==========

        gameFlowCoordinator.Initialize(
            roomManager,
            transitionController,
            uiManager
        );

        transitionController.Initialize(playerManager);

        roomManager.Initialize(transitionController);

        playerManager.Initialize(roomManager);

        shopManager.Initialize(inventoryManager);


        // ========== 创建并初始化服务 ==========

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

        SlotService = new SlotClearService();

        // ========== 注册所有服务到 ServiceLocator ==========

        RegisterToServiceLocator();

        // ========== 订阅服务的事件 ==========

        FloorRewardSystemService.Subscribe();
        PassiveCardApplicationService.Subscribe();
        RoomPlayerSkillLimitService.Subscribe();
        SlotService.Subscribe();

        // ========== 初始化 UI 控制器 ==========

        InitializeUIControllers();

        // ========== 初始化进化系统 ==========

        InitializeEvolutionSystem();

        // ========== 输出服务注册状态 ==========

        if (enableDebugLogs)
        {
            CDLogger.Log(ServiceLocator.GetDebugInfo());
        }

        // // 启动时加载元游戏存档
        // SaveManager.LoadMeta();

        CDLogger.Log("[GameRoot] 初始化完成");
    }

    /// <summary>
    /// 初始化全局 UI 控制器
    /// 这些控制器在游戏启动时就开始工作，确保事件不会因 UI 未加载而丢失
    /// </summary>
    private void InitializeUIControllers()
    {
        // 技能进化 UI 控制器（纯 C#，避免事件丢失）
        SkillEvolutionUIController.Initialize(uiManager);
        CDLogger.Log("[GameRoot] SkillEvolutionUIController 已初始化");
    }

    /// <summary>
    /// 初始化技能进化系统
    /// </summary>
    private void InitializeEvolutionSystem()
    {
        // 初始化效果池
        if (evolutionEffectPool != null)
        {
            evolutionEffectPool.Initialize();
            CDLogger.Log($"[GameRoot] EvolutionEffectPool 已初始化 - {evolutionEffectPool.GetPoolStatistics()}");
        }
        else
        {
            Debug.LogError("[GameRoot] EvolutionEffectPool 未配置！请在 Inspector 中分配。");
        }

        // 初始化升级服务（需要效果池引用）
        inventoryManager.ActiveCardUpgradeService.Initialize();
    }

    /// <summary>
    /// 将所有管理器和服务注册到 ServiceLocator
    ///
    /// 注册约定：
    /// - 管理器（Unity Component）：使用 "Manager" 后缀
    /// - 服务（纯 C# 类）：使用 "Service" 后缀
    /// - 可选命名：用于同类型多实例场景
    /// </summary>
    private void RegisterToServiceLocator()
    {
        // ========== 核心管理器 ==========
        // 这些是 Unity 单例，保留直接访问方式，同时也注册到 ServiceLocator

        ServiceLocator.Register(gameFlowCoordinator);
        ServiceLocator.Register(roomManager);
        ServiceLocator.Register(uiManager);
        ServiceLocator.Register(transitionController);
        ServiceLocator.Register(playerManager);
        ServiceLocator.Register(inventoryManager);
        ServiceLocator.Register(shopManager);
        ServiceLocator.Register(gameInput);

        // ========== Inventory 子服务 ==========
        // 注册 inventoryManager 的子服务，使其可独立通过 ServiceLocator 访问
        // 注意：此时 inventoryManager.Awake() 已执行，子服务已创建完成
        ServiceLocator.Register(inventoryManager.CoinService);
        ServiceLocator.Register(inventoryManager.ActiveCardService);
        ServiceLocator.Register(inventoryManager.PassiveCardService);
        ServiceLocator.Register(inventoryManager.ActiveCardUpgradeService);

        // ========== 核心服务 ==========
        // 纯 C# 服务，主要通过 ServiceLocator 访问

        ServiceLocator.Register(FloorRewardSystemService);
        ServiceLocator.Register(DifficultyService);
        ServiceLocator.Register(PassiveCardApplicationService);
        ServiceLocator.Register(RoomPlayerSkillLimitService);
        ServiceLocator.Register(CombatRewardEnergyService);
        ServiceLocator.Register(SlotService);

        // ========== 配置（可选） ==========
        // 如果需要通过 ServiceLocator 访问配置，也可以注册
        // ServiceLocator.Register(cardDatabase);
        // ServiceLocator.Register(difficultyCurveConfig);
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
        SlotService?.Unsubscribe();
        RoomPlayerSkillLimitService?.Unsubscribe();
        PassiveCardApplicationService?.Unsubscribe();
        FloorRewardSystemService?.Unsubscribe();

        // 可选：清空 ServiceLocator（主要用于场景切换时）
        // ServiceLocator.Clear();

        base.OnDestroy();
    }

    /// <summary>
    /// 快捷访问：获取 GameRoot 实例（比 Instance 更语义化）
    /// </summary>
    public static GameRoot I => Instance;
}
