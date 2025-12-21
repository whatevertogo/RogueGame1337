using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CheckIfHover : MonoBehaviour
{
    public UIHoverAnimator uiHoverAnimator;
    Image image;

    void Awake()
    {
        image = GetComponent<Image>();

        if (!uiHoverAnimator)
        {
            Debug.LogError($"[CheckIfHover] UIHoverAnimator not found in parent of {name}");
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (uiHoverAnimator != null)
            uiHoverAnimator.Hoveraction += OnHover;
    }

    void OnDisable()
    {
        if (uiHoverAnimator != null)
            uiHoverAnimator.Hoveraction -= OnHover;
    }

    void OnHover(bool isHover)
    {
        if (isHover)
            Show();
        else
            Hide();
    }

public void Show()
{
    image.DOFade(1f, 0.2f);
}

public void Hide()
{
    image.DOFade(0f, 0.2f);
}
}
