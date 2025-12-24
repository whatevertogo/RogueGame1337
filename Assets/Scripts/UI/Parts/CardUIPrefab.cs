using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class CardUIPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [ReadOnly]
    public string CardId;

    public Image image;
    [SerializeField] private TextMeshProUGUI amountText;

    [HideInInspector] public Transform originalParent;
    private Vector3 originalPosition;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private Vector3 dragScale = Vector3.one * 1.1f;

    public CardUIPrefab Init(string cardId, int amount)
    {
        CardId = cardId;

        if (image == null) image = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        if (amountText == null) amountText = GetComponentInChildren<TextMeshProUGUI>();

        var card = GameRoot.Instance.CardDatabase.Resolve(cardId);
        if (card != null)
        {
            image.sprite = card.GetSprite();
            if (amountText != null)
                amountText.text = amount.ToString();
        }

        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        return this;
    }

    #region Drag Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = transform.position;
        originalParent = transform.parent;

        transform.SetParent(parentCanvas.transform, true);
        transform.SetAsLastSibling();
        transform.DOScale(dragScale, 0.2f).SetEase(Ease.OutBack);

        canvasGroup.blocksRaycasts = false;


        CDTU.Utils.CDLogger.Log($"[CardUIPrefab] BeginDrag: {CardId}, originalParent={originalParent.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position += (Vector3)eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        bool droppedOnSlot = transform.parent.TryGetComponent<CardSlot>(out var slot);

        if (!droppedOnSlot)
        {
            // 回到原位
            transform.DOMove(originalPosition, 0.25f).SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    transform.SetParent(originalParent, true);
                    transform.localScale = Vector3.one;
                });
        }
        else
        {
            // 放到槽位时，由CardSlot.OnDrop处理动画，这里只重置缩放
            transform.localScale = Vector3.one;
        }
    }

    #endregion
}
