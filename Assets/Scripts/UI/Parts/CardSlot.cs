using UnityEngine;
using UnityEngine.EventSystems;
using RogueGame.Events;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class CardSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int SlotIndex;
    private string _cardID;
    public string CardID
    {
        get => _cardID;
        set
        {
            if (_cardID == value) return;
            _cardID = value;
            

            EventBus.Publish(new OnPlayerSkillEquippedEvent(GameRoot.Instance.PlayerManager.GetLocalPlayerData().PlayerId,SlotIndex,CardID));
        }
    }

    private Sprite _defaultSprite;
    private void Awake()
    {
        var img = GetComponent<Image>();
        if (img != null) _defaultSprite = img.sprite;
    }


    private void Reset()
    {
        var img = GetComponent<Image>();
        img.raycastTarget = true;

        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (!eventData.pointerDrag.TryGetComponent<CardUIPrefab>(out var draggedCard)) return;

        // 检查当前槽位是否已有卡牌（排除拖拽的卡牌本身）
        CardUIPrefab existingCard = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child != draggedCard.transform && child.TryGetComponent<CardUIPrefab>(out var card))
            {
                existingCard = card;
                break;
            }
        }

        // 获取原始槽位（如果有）
        CardSlot originalSlot = null;
        if (draggedCard.originalParent != null)
            draggedCard.originalParent.TryGetComponent<CardSlot>(out originalSlot);

        // 如果槽位已有卡牌，执行交换
        if (existingCard != null)
        {
            // 确保原始槽位是CardSlot，才允许交换
            if (originalSlot != null)
            {
                // 交换卡牌位置
                existingCard.transform.SetParent(draggedCard.originalParent, false);
                existingCard.transform.localScale = Vector3.one;
                existingCard.transform.localPosition = Vector3.zero;

                draggedCard.transform.SetParent(transform, false);
                draggedCard.transform.localScale = Vector3.one;
                draggedCard.transform.DOLocalMove(Vector3.zero, 0.25f).SetEase(DG.Tweening.Ease.OutCubic);

                // 同时更新两个槽位的ID
                originalSlot.CardID = existingCard.CardId;
                CardID = draggedCard.CardId;
            }
            else
            {
                // 原位置不是槽位，不允许放入已满的槽
                CDTU.Utils.Logger.Log($"[CardSlot] 槽位已满，无法放入卡牌");
            }
        }
        else
        {
            // 槽位为空，直接放入
            draggedCard.transform.SetParent(transform, false);
            draggedCard.transform.localScale = Vector3.one;
            draggedCard.transform.DOLocalMove(Vector3.zero, 0.25f).SetEase(DG.Tweening.Ease.OutCubic);

            // 清空原槽位（如果有）
            if (originalSlot != null)
                originalSlot.CardID = null;
            CardID = draggedCard.CardId;
        }
    }

    /// <summary>
    /// 清空槽位内的卡牌 UI（销毁 CardUIPrefab 子对象）并触发槽位变更事件
    /// </summary>
    public void ClearSlot()
    {
        // 销毁所有 CardUIPrefab 子对象
        var cards = GetComponentsInChildren<CardUIPrefab>(true);
        for (int i = cards.Length - 1; i >= 0; i--)
        {
            var go = cards[i].gameObject;
            if (Application.isPlaying)
                GameObject.Destroy(go);
            else
                GameObject.DestroyImmediate(go);
        }

        // 触发槽位 ID 变化（setter 会发布事件）
        CardID = null;
    }
}
