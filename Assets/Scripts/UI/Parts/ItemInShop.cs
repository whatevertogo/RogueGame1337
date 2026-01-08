using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemInShop : MonoBehaviour
{
    public ItemType itemType = ItemType.None;
    public GameObject messageUIDisplay;
    private CanvasGroup MessageCanvasGroup;

    void Start()
    {
        if (messageUIDisplay != null)
        {
            MessageCanvasGroup = messageUIDisplay.GetComponent<CanvasGroup>();
            if (MessageCanvasGroup != null)
            {
                MessageCanvasGroup.alpha = 0f; // 初始时隐藏价格UI
                messageUIDisplay.transform.localPosition += new Vector3(0, -25f, 0); // 初始位置下移一点
            }
        }

        if (itemType == ItemType.None)
        {
            Debug.LogError("ItemInShop: itemType is not set!");
        }
        else if (itemType == ItemType.Card)
        {
            var newcardId = GameRoot.I.CardDatabase.GetRandomCardId();
            messageUIDisplay.GetComponentInChildren<IMessageUIDisplay>().Init(GameRoot.I.CardDatabase.Resolve(newcardId).CardId);

            
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 使用 DOTween 实现淡入并向上飘动
            MessageCanvasGroup.DOFade(1, 0.2f);
            messageUIDisplay.transform.DOLocalMoveY(25f, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 淡出并回落
            MessageCanvasGroup.DOFade(0, 0.2f);
            messageUIDisplay.transform.DOLocalMoveY(-25f, 0.2f);
        }
    }




}
