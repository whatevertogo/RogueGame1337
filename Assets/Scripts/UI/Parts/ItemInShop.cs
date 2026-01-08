using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CardInShop : MonoBehaviour
{
    public ItemType itemType =ItemType.None;

    public GameObject priceUIDisplay;

    private CanvasGroup priceCanvasGroup;

    void Start()
    {
        if (priceUIDisplay != null)
        {
            priceCanvasGroup = priceUIDisplay.GetComponent<CanvasGroup>();
            if (priceCanvasGroup != null)
            {
                priceCanvasGroup.alpha = 0f; // 初始时隐藏价格UI
                priceUIDisplay.transform.localPosition += new Vector3(0, -0.2f, 0); // 初始位置下移一点
            }
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 使用 DOTween 实现淡入并向上飘动
            priceCanvasGroup.DOFade(1, 0.2f);
            priceUIDisplay.transform.DOLocalMoveY(0.5f, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 淡出并回落
            priceCanvasGroup.DOFade(0, 0.2f);
            priceUIDisplay.transform.DOLocalMoveY(0.3f, 0.2f);
        }
    }




}
