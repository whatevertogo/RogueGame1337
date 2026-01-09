using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// ComboUIView View 层 - UI 组件绑定和显示方法
    /// </summary>
    public partial class ComboUIView : UIViewBase
    {
        [Header("连击显示组件")]
        [SerializeField]
        [Tooltip("连击数字文本")]
        private TMP_Text comboCountText;

        [SerializeField]
        [Tooltip("档位名称文本")]
        private TMP_Text tierNameText;

        [SerializeField]
        [Tooltip("时间进度条" )]
        private Image timeProgressBar;

        [Header("动画配置")]
        [SerializeField]
        private float comboCountPunchScale = 1.3f; // 连击数字跳动放大倍数

        [SerializeField]
        private float comboCountPunchDuration = 0.2f; // 连击数字跳动持续时间

        [SerializeField]
        private float tierNameFlashDuration = 0.5f; // 档位名称闪烁持续时间

        [SerializeField]
        private float warningFlashInterval = 0.3f; // 警告闪烁间隔

        // 协程引用（用于停止警告动画）
        private Coroutine _warningFlashCoroutine;

        public override bool Exclusive => false;
        public override bool CanBack => false;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定（使用 @ 前缀命名）
        }

        private void OnDisable()
        {
            // 停止所有协程
            StopAllCoroutines();
            _warningFlashCoroutine = null;
        }

        // ═══════════════════════════════════════════════════════════════
        // 文本更新方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 更新连击数字文本
        /// </summary>
        public void SetComboCountText(string content)
        {
            if (comboCountText != null)
                comboCountText.text = content;
        }

        /// <summary>
        /// 更新档位名称文本
        /// </summary>
        public void SetTierNameText(string content)
        {
            if (tierNameText != null)
                tierNameText.text = content;
        }

        // ═══════════════════════════════════════════════════════════════
        // 颜色更新方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 更新连击数字颜色
        /// </summary>
        public void SetComboCountColor(Color color)
        {
            if (comboCountText != null)
                comboCountText.color = color;
        }

        /// <summary>
        /// 更新档位名称颜色
        /// </summary>
        public void SetTierNameColor(Color color)
        {
            if (tierNameText != null)
                tierNameText.color = color;
        }

        /// <summary>
        /// 更新进度条颜色
        /// </summary>
        public void SetProgressBarColor(Color color)
        {
            if (timeProgressBar != null)
                timeProgressBar.color = color;
        }

        // ═══════════════════════════════════════════════════════════════
        // 进度条更新方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 更新时间进度条（0.0 - 1.0）
        /// 注意：此方法依赖 Image 的 Filled 模式，必须在 Inspector 中设置：
        /// - Image Type: Filled
        /// - Fill Method: Horizontal
        /// - Fill Origin: Left
        /// </summary>
        /// <param name="fillAmount">填充量（0.0 = 空，1.0 = 满）</param>
        public void SetProgressBarFill(float fillAmount)
        {
            if (timeProgressBar != null)
                timeProgressBar.fillAmount = Mathf.Clamp01(fillAmount);
        }

        // ═══════════════════════════════════════════════════════════════
        // 动画方法（使用 DOTween）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 播放连击数字跳动动画（放大后回弹）
        /// </summary>
        public void PlayComboCountPunchAnimation()
        {
            if (comboCountText != null)
            {
                comboCountText.transform.DOPunchScale(
                    Vector3.one * comboCountPunchScale,
                    comboCountPunchDuration
                );
            }
        }

        /// <summary>
        /// 播放档位名称闪烁动画（颜色 + 缩放）
        /// 使用 UGUI 协程实现，避免 DOTween API 兼容性问题
        /// </summary>
        public void PlayTierNameFlashAnimation(Color flashColor)
        {
            if (tierNameText != null)
            {
                StartCoroutine(TierNameFlashCoroutine(flashColor));
            }
        }

        /// <summary>
        /// 档位名称闪烁协程
        /// </summary>
        private IEnumerator TierNameFlashCoroutine(Color flashColor)
        {
            if (tierNameText == null)
                yield break;

            Color originalColor = tierNameText.color;
            float halfDuration = tierNameFlashDuration * 0.5f;
            int flashCount = 2; // 闪烁次数

            // 颜色闪烁
            for (int i = 0; i < flashCount; i++)
            {
                tierNameText.color = flashColor;
                yield return new WaitForSeconds(halfDuration);

                tierNameText.color = originalColor;
                yield return new WaitForSeconds(halfDuration);
            }

            tierNameText.color = originalColor;

            // 同步播放缩放动画（使用 DOTween）
            tierNameText.transform.DOPunchScale(Vector3.one * 0.3f, tierNameFlashDuration);
        }

        /// <summary>
        /// 播放进度条警告闪烁动画（无限循环，直到调用 Stop）
        /// 使用 UGUI 协程实现，避免 DOTween API 兼容性问题
        /// </summary>
        public void PlayProgressBarWarningAnimation(Color warningColor)
        {
            if (timeProgressBar != null)
            {
                // 停止之前的警告动画
                if (_warningFlashCoroutine != null)
                {
                    StopCoroutine(_warningFlashCoroutine);
                }

                // 启动新的警告动画
                _warningFlashCoroutine = StartCoroutine(WarningFlashCoroutine(warningColor));
            }
        }

        /// <summary>
        /// 警告闪烁协程（无限循环）
        /// </summary>
        private IEnumerator WarningFlashCoroutine(Color warningColor)
        {
            if (timeProgressBar == null)
                yield break;

            Color originalColor = timeProgressBar.color;
            bool isWarningColor = false;

            while (true)
            {
                isWarningColor = !isWarningColor;
                timeProgressBar.color = isWarningColor ? warningColor : originalColor;
                yield return new WaitForSeconds(warningFlashInterval);
            }
        }

        /// <summary>
        /// 停止进度条闪烁动画
        /// </summary>
        public void StopProgressBarWarningAnimation(Color normalColor)
        {
            // 停止协程
            if (_warningFlashCoroutine != null)
            {
                StopCoroutine(_warningFlashCoroutine);
                _warningFlashCoroutine = null;
            }

            // 恢复颜色
            if (timeProgressBar != null)
            {
                timeProgressBar.color = normalColor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 显示/隐藏控制
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 设置连击 UI 可见性（连击为 0 时隐藏）
        /// </summary>
        public void SetComboVisible(bool visible)
        {
            if (comboCountText != null)
                comboCountText.gameObject.SetActive(visible);

            if (tierNameText != null)
                tierNameText.gameObject.SetActive(visible);

            if (timeProgressBar != null)
                timeProgressBar.gameObject.SetActive(visible);
        }
    }
}
