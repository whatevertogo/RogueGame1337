using UnityEngine;
using UnityEngine.UI;

#if DOTWEEN_EXISTS
using DG.Tweening;
#endif

namespace CDTU.Utils.TweenUtils
{
    public static class MyTweenUtils
    {
        /// <summary>
        /// 渐显（FadeIn）
        /// </summary>
        /// <param name="canvasGroup">要渐显的 CanvasGroup</param>
        /// <param name="duration">持续时间</param>
        /// <param name="targetAlpha">目标透明度，默认 1f</param>
        /// <returns>返回 Tweener 可用于链式调用或控制</returns>
        public static object FadeIn(CanvasGroup canvasGroup, float duration, float targetAlpha = 1f)
        {
            if (canvasGroup == null) return null;

#if DOTWEEN_EXISTS
            return canvasGroup.DOFade(targetAlpha, duration);
#else
            Debug.LogWarning("[TweenUtils] DOTween is not available. FadeIn skipped.");
            return null;
#endif
        }

        /// <summary>
        /// 渐隐（FadeOut）
        /// </summary>
        /// <param name="canvasGroup">要渐隐的 CanvasGroup</param>
        /// <param name="duration">持续时间</param>
        /// <param name="targetAlpha">目标透明度，默认 0f</param>
        /// <returns>返回 Tweener 可用于链式调用或控制</returns>
        public static object FadeOut(CanvasGroup canvasGroup, float duration, float targetAlpha = 0f)
        {
            if (canvasGroup == null) return null;

#if DOTWEEN_EXISTS
            return canvasGroup.DOFade(targetAlpha, duration);
#else
            Debug.LogWarning("[TweenUtils] DOTween is not available. FadeOut skipped.");
            return null;
#endif
        }
    }
}
