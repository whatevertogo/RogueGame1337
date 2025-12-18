using CardSystem;
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
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TransitionController transitionController;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private LootDropper lootDropper;

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
