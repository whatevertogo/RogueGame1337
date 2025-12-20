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

        cardDatabase.Initialize();

        gameStateManager.Initialize(
            roomManager,
            transitionController,
            uiManager
        );
        roomManager.Initialize(transitionController);

        playerManager.Initialize(roomManager);  

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
}
