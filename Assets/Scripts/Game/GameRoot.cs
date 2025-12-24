using CDTU.Utils;
using RogueGame.Map;
using RogueGame.SaveSystem;
using UI;
using UnityEngine;

/// <summary>
/// 游戏根节点，管理全局单例和核心系统
/// </summary>
public class GameRoot : Singleton<GameRoot>
{
    [Header("Game DataBases")]
    [InlineEditor]
    [SerializeField] private CardDataBase cardDatabase;
    public CardDataBase CardDatabase => cardDatabase;

    [Header("Game Configs")]
    [InlineEditor, Tooltip("游戏充能平衡配置")]
    [SerializeField] private GameChargeBalanceConfig GameBalanceConfig;

    //技能充能Config
    [InlineEditor, Tooltip("游戏充能平衡配置")]
    public GameChargeBalanceConfig ChargeBalanceConfig => GameBalanceConfig;

    //游戏胜利奖励配置
    [InlineEditor, Tooltip("层间胜利奖励配置")]
    [SerializeField] private GameWinLayerRewardConfig gameWinLayerRewardConfig;
    public GameWinLayerRewardConfig GameWinLayerRewardConfig => gameWinLayerRewardConfig;

    //主动卡去重配置
    [InlineEditor, Tooltip("重复主动卡转换为金币的配置")]
    [SerializeField] private ActiveCardDeduplicationConfig activeCardDeduplicationConfig;
    public ActiveCardDeduplicationConfig ActiveCardDeduplicationConfig => activeCardDeduplicationConfig;

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
    [SerializeField] private InventoryManager inventoryManager;

    public InventoryManager InventoryManager => inventoryManager;
    [SerializeField] private LootDropper lootDropper;
    public LootDropper LootDropper => lootDropper;
    [SerializeField] private SaveManager saveManager;
    public SaveManager SaveManager => saveManager;

    [SerializeField] private ShopManager shopManager;
    public ShopManager ShopManager => shopManager;

    [SerializeField] private GameInput gameInput;

    public GameInput GameInput => gameInput;

    //Services
    // SlotService放UI根对象了
    // public SlotService SlotService => GetComponent<SlotService>();

    //战斗奖励能量服务
    public CombatRewardEnergyService CombatRewardEnergyService { get; private set; }

    //战斗奖励技能服务
    public SkillChargeSyncService SkillChargeSyncService { get; private set; }

    //层间奖励系统服务
    public FloorRewardSystemService FloorRewardSystemService { get; private set; }


    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[GameRoot] Awake()");

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
        ok &= AssertNotNull(GameBalanceConfig, nameof(GameBalanceConfig));
        ok &= AssertNotNull(gameWinLayerRewardConfig, nameof(gameWinLayerRewardConfig));
        ok &= AssertNotNull(activeCardDeduplicationConfig, nameof(activeCardDeduplicationConfig));



        if (!ok)
        {
            Debug.LogError("[GameRoot] Initialization aborted due to missing references.");
            return;
        }

        // 确保 SlotService 在运行时存在，用于处理槽位相关集中逻辑（例如响应 ClearAllSlotsRequestedEvent）
        if (GetComponent<SlotService>() == null)
        {
            gameObject.AddComponent<SlotService>();
            Debug.Log("[GameRoot] SlotService added to GameRoot at runtime");
        }

        Debug.Log("[GameRoot] All required references assigned. Initializing CardDatabase.");
        cardDatabase.Initialize();

        gameFlowCoordinator.Initialize(
            roomManager,
            transitionController,
            uiManager
        );
        roomManager.Initialize(transitionController);

        playerManager.Initialize(roomManager);

        //SaveManager 初始化依赖于 PlayerManager 和 InventoryManager

        shopManager.Initialize(inventoryManager);

        //Serivces 初始化
        CombatRewardEnergyService = new CombatRewardEnergyService(
            cardDatabase,
            inventoryManager,
            GameBalanceConfig
        );

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


        FloorRewardSystemService.Subscribe();

        // 启动时加载元游戏存档
        SaveManager.LoadMeta();
    }

    private bool AssertNotNull(Object obj, string name)
    {
        if (obj == null)
        {
            Debug.LogError($"[GameRoot] {name} is not assigned.");
            return false;
        }
        return true;
    }


    public void OnDestroy()
    {
        Debug.Log("[GameRoot] OnDestroy() called.");

        // 取消订阅 FloorRewardSystemService 的事件
        FloorRewardSystemService?.Unsubscribe();
    }
}
