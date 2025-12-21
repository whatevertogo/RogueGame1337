using System;
using CDTU.Utils;
using RogueGame.Map;
using RogueGame.SaveSystem;
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
    // [SerializeField] private SaveManager saveManager;
    // public SaveManager SaveManager => saveManager;

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

        gameStateManager.Initialize(
            roomManager,
            transitionController,
            uiManager
        );
        roomManager.Initialize(transitionController);

        playerManager.Initialize(roomManager);  

        // saveManager.Initialize(gameStateManager, playerManager);

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
