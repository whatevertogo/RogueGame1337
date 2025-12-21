using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverAnimator : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private Animator animator;
    private static readonly int IsHoverKey = Animator.StringToHash("isHover");

    public event Action<bool> Hoveraction;
    void Awake()
    {
        TryGetComponent(out animator);

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hoveraction?.Invoke(true);
        if(animator is null) return;
        animator.SetBool(IsHoverKey, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hoveraction?.Invoke(false);
        if(animator is null) return;
        animator.SetBool(IsHoverKey, false);
    }
}
