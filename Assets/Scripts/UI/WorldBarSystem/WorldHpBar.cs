
using UnityEngine;
using UnityEngine.UI;

public class WorldHpBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public RectTransform RectTransform => transform as RectTransform;

    private void Awake()
    {
        if (fillImage == null)
        {
            var t = transform.Find("Fill");
            if (t != null) fillImage = t.GetComponent<Image>();
        }
        if (fillImage == null)
        {
            CDTU.Utils.Logger.LogWarning("[WorldHpBar] fillImage 未绑定");
        }
    }

    public void SetPercent(float percent)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(percent);
    }

    public void SetPercentSmooth(float percent, float duration)
    {
        // 简单线性 lerp over duration — 若项目引入 DOTween，可替换为更平滑动画
        // 这里为占位实现：直接设置（可扩展）
        SetPercent(percent);
    }

}