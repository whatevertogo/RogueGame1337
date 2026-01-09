using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemInShop : MonoBehaviour, IInteractable
{
    public ItemType itemType = ItemType.None;
    public GameObject messageUIDisplay;
    private CanvasGroup MessageCanvasGroup;
    private IMessageUIDisplay _messageUI;
    private float _originalLocalY;
    private CardDefinition card;

    void Start()
    {
        if (messageUIDisplay == null)
        {
            Debug.LogWarning("ItemInShop: messageUIDisplay is null on " + gameObject.name);
        }
        else
        {
            MessageCanvasGroup = messageUIDisplay.GetComponent<CanvasGroup>();
            _messageUI = messageUIDisplay.GetComponentInChildren<IMessageUIDisplay>();
            // 记录原始 localY 以便做绝对位置移动
            _originalLocalY = messageUIDisplay.transform.localPosition.y;

            if (MessageCanvasGroup != null)
            {
                MessageCanvasGroup.alpha = 0f; // 初始时隐藏价格UI
            }
            // 初始位置下移 25（相对于原始位置）
            messageUIDisplay.transform.localPosition = new Vector3(
                messageUIDisplay.transform.localPosition.x,
                _originalLocalY - 25f,
                messageUIDisplay.transform.localPosition.z
            );
        }

        switch (itemType)
        {
            case ItemType.None:
                Debug.LogError("ItemInShop: itemType is not set!");
                break;
            case ItemType.Card:
                var newcardId = GameRoot.I.CardDatabase.GetRandomCardId();
                card = GameRoot.I.CardDatabase.Resolve(newcardId);
                string Message =
                    $"卡牌ID:{card.CardId}\n"
                    +
                    //  $"稀有度: {card.Rarity}\n" +
                    //  $"类型: {card.Type}\n" +
                    $"费用: {card.Cost}\n"
                    + $"描述: {card.GetDescription()}\n"
                    + $"按F键购买";
                if (_messageUI != null)
                {
                    _messageUI.Init(Message);
                }
                else if (messageUIDisplay != null)
                {
                    Debug.LogWarning(
                        "ItemInShop: IMessageUIDisplay component not found on "
                            + messageUIDisplay.name
                    );
                }
                break;
            //TODO-遗物系统
            // case ItemType.Consumable:
            //     // 设置遗物信息
            //     break;
            default:
                break;
        }
    }

    private void MoveOver()
    {
        // 防止残留 tween 叠加
        messageUIDisplay.transform.DOKill();
        if (MessageCanvasGroup != null)
            MessageCanvasGroup.DOFade(1, 0.2f);
        // 将下移的 UI 提升回原始位置（相对于 Start 时向下 25）
        messageUIDisplay.transform.DOLocalMoveY(_originalLocalY, 0.3f).SetEase(Ease.OutBack);
    }

    private void MoveOverBack()
    {
        // 防止残留 tween 叠加
        messageUIDisplay.transform.DOKill();
        if (MessageCanvasGroup != null)
            MessageCanvasGroup.DOFade(0, 0.2f);
        // 回落到初始下移状态
        messageUIDisplay.transform.DOLocalMoveY(_originalLocalY - 25f, 0.2f);
    }

    #region Interactable方法

    public void Interact(GameObject interactor)
    {
        Debug.Log($"购买了物品: {itemType} 来自 {gameObject.name}");
        //TODO- 这里添加购买逻辑，比如扣除金币，添加物品到背包等
        GameRoot.I.ShopManager.BuyCards(card.Cost);
        Destroy(gameObject); // 购买后销毁物品
    }

    public void OnPlayerEnter(GameObject interactor)
    {
        MoveOver();
    }

    public void OnPlayerExit(GameObject interactor)
    {
        MoveOverBack();
    }
    #endregion
}
