using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace UI.Combat
{
    /// <summary>
    /// 伤害数字显示组件
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [Header("显示设置")]
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float moveUpDistance = 2f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("颜色设置")]
        [SerializeField] private Color normalDamageColor = Color.white;
        [SerializeField] private Color critDamageColor = Color.red;
        [SerializeField] private Color healColor = Color.green;
        [SerializeField] private Color missColor = Color.gray;

        private RectTransform _rectTransform;
        private Vector3 _initialPosition;
        private Camera _mainCamera;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialPosition = _rectTransform.localPosition;
            _mainCamera = Camera.main;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 显示伤害数字
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="isCrit">是否暴击</param>
        /// <param name="isHeal">是否治疗</param>
        /// <param name="isMiss">是否未命中</param>
        public void ShowDamage(int damage, bool isCrit = false, bool isHeal = false, bool isMiss = false)
        {
            if (damageText == null) return;

            // 设置文本内容
            string displayText = isMiss ? "Miss" : (isHeal ? $"+{damage}" : damage.ToString());
            damageText.SetText(displayText);

            // 设置颜色
            Color displayColor = normalDamageColor;
            if (isMiss) displayColor = missColor;
            else if (isHeal) displayColor = healColor;
            else if (isCrit) displayColor = critDamageColor;

            damageText.color = displayColor;

            // 设置大小（暴击时更大）
            float scale = isCrit ? 1.5f : 1f;
            _rectTransform.localScale = Vector3.one * scale;

            // 开始显示动画
            StartCoroutine(DisplayAnimation());
        }

        /// <summary>
        /// 显示动画
        /// </summary>
        private IEnumerator DisplayAnimation()
        {
            // 重置状态
            _rectTransform.localPosition = _initialPosition;
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            // 等待一帧确保UI正确设置
            yield return null;

            // 向上移动并淡出
            float elapsedTime = 0f;
            Vector3 targetPosition = _initialPosition + Vector3.up * moveUpDistance;

            while (elapsedTime < displayDuration)
            {
                elapsedTime += Time.deltaTime;

                // 计算进度
                float progress = elapsedTime / displayDuration;

                // 使用 displayDuration 与 fadeOutDuration 计算移动与淡出时段
                Vector3 currentPosition;
                float fadeStartTime = Mathf.Max(0f, displayDuration - fadeOutDuration);

                if (elapsedTime < fadeStartTime && fadeStartTime > 0f)
                {
                    float moveProgress = elapsedTime / fadeStartTime;
                    currentPosition = Vector3.Lerp(_initialPosition, targetPosition, moveProgress);
                }
                else
                {
                    currentPosition = targetPosition;
                }

                _rectTransform.localPosition = currentPosition;

                // 淡出（从 fadeStartTime 开始，持续 fadeOutDuration 秒）
                if (elapsedTime > fadeStartTime && canvasGroup != null && fadeOutDuration > 0f)
                {
                    float fadeProgress = Mathf.Clamp01((elapsedTime - fadeStartTime) / fadeOutDuration);
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeProgress);
                }

                yield return null;
            }

            // 完全隐藏
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            // 回收对象
            DamageNumberManager.Instance?.ReturnToPool(this);
        }

        /// <summary>
        /// 重置到初始状态
        /// </summary>
        public void Reset()
        {
            StopAllCoroutines();
            _rectTransform.localPosition = _initialPosition;
            _rectTransform.localScale = Vector3.one;

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (damageText != null) damageText.color = normalDamageColor;
        }

        /// <summary>
        /// 设置朝向摄像机的方向
        /// </summary>
        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }
        }
    }
}