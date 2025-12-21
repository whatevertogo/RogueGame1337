using System;
using CDTU.Utils;
using RogueGame.Map;
using UI;
using UnityEngine;

public class GameRoot : Singleton<GameRoot>
{
    [Header("Game DataBases")]
    [InlineEditor]
    [SerializeField] private CardDataBase cardDatabase;
    public CardDataBase CardDatabase => cardDatabase;

    [Header("Scene Managers")]
    [SerializeField] private GameStateManager gameStateManager;
    public GameStateManager GameStateManager => gameStateManager;
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

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[GameRoot] Awake()");

        bool ok = true;
        ok &= AssertNotNull(cardDatabase, nameof(cardDatabase));
        ok &= AssertNotNull(gameStateManager, nameof(gameStateManager));
        ok &= AssertNotNull(roomManager, nameof(roomManager));
        ok &= AssertNotNull(uiManager, nameof(uiManager));
        ok &= AssertNotNull(transitionController, nameof(transitionController));
        ok &= AssertNotNull(playerManager, nameof(playerManager));
        ok &= AssertNotNull(inventoryManager, nameof(inventoryManager));

        if (!ok)
        {
            // 如果有缺失引用，记录并在 cardDatabase 可用时强制初始化数据库，便于调试和最小功能运行
            Debug.LogError("[GameRoot] Initialization aborted due to missing references.");
            if (cardDatabase != null)
            {
                try
                {
                    Debug.LogWarning("[GameRoot] Some references missing but CardDatabase assigned — initializing CardDatabase for debug.");
                    cardDatabase.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameRoot] CardDatabase.Initialize() threw: {ex}");
                }
            }
            return;
        }

        Debug.Log("[GameRoot] All required references assigned. Initializing CardDatabase.");
        cardDatabase.Initialize();

        gameStateManager.Initialize(
            roomManager,
            transitionController,
            uiManager
        );
        roomManager.Initialize(transitionController);

        playerManager.Initialize(roomManager);  

    }

    private bool AssertNotNull(UnityEngine.Object obj, string name)
    {
        if (obj == null)
        {
            Debug.LogError($"[GameRoot] {name} is not assigned.");
            return false;
        }
        return true;
    }
}
