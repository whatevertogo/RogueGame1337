using System;
using Core.Events;
using RogueGame.Events;
using UI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// ComboUIView 纯逻辑核心（可单元测试）
    /// 职责：订阅连击事件并驱动 UI 更新
    /// </summary>
    public class ComboUIViewLogicCore
    {
        protected ComboUIView _view;

        // 连击状态缓存
        private int _currentCombo = 0;
        private ComboTier _currentTier;
        private float _remainingTime = 0f;
        private float _comboWindow = 5f; // 从 ComboConfigSO 获取

        // 警告状态标记
        private bool _isWarningState = false;

        public virtual void Bind(UIViewBase view)
        {
            _view = view as ComboUIView;
        }

        public virtual void OnOpen(UIArgs args)
        {
            // 订阅连击事件
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<ComboTierChangedEvent>(OnTierChanged);
            EventBus.Subscribe<ComboExpiredEvent>(OnComboExpired);

            // 初始化 UI：隐藏连击显示（初始连击为 0）
            if (_view != null)
                _view.SetComboVisible(false);

            // 获取 ComboConfigSO 配置
            if (ComboManager.Instance != null)
            {
                // 假设 ComboManager 暴露了 ComboConfigSO
                // 如果没有暴露，可以硬编码或通过其他方式获取
                _comboWindow = 5f; // 默认值
            }
        }

        public virtual void OnClose()
        {
            // 退订事件
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<ComboTierChangedEvent>(OnTierChanged);
            EventBus.Unsubscribe<ComboExpiredEvent>(OnComboExpired);

            // 停止所有 DOTween 动画
            if (_view != null)
                _view.StopProgressBarWarningAnimation(Color.white);

            _view = null;
        }

        public virtual void OnCovered()
        {
            // 被同层新 UI 覆盖时的默认处理（子类可重写）
        }

        public virtual void OnResume()
        {
            // 从覆盖状态恢复时的默认处理（子类可重写）
        }

        // ═══════════════════════════════════════════════════════════════
        // 事件处理方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 处理连击变化事件（每次击杀触发）
        /// </summary>
        private void OnComboChanged(ComboChangedEvent evt)
        {
            Debug.Log($"[ComboUI] 收到连击事件: Combo={evt.CurrentCombo}, Tier={evt.ComboTier.comboState}");

            _currentCombo = evt.CurrentCombo;
            _currentTier = evt.ComboTier;

            // 连击为 0 时隐藏 UI
            if (_currentCombo == 0)
            {
                if (_view != null)
                    _view.SetComboVisible(false);
                _isWarningState = false;
                return;
            }

            // 显示 UI
            if (_view != null)
                _view.SetComboVisible(true);

            // 更新连击数字
            if (_view != null)
                _view.SetComboCountText(_currentCombo.ToString());

            // 更新颜色
            if (_view != null)
            {
                _view.SetComboCountColor(_currentTier.tierColor);
                _view.SetTierNameColor(_currentTier.tierColor);
                _view.SetProgressBarColor(_currentTier.tierColor);
            }

            // 更新档位名称
            if (_view != null)
                _view.SetTierNameText(_currentTier.comboState.ToString());

            // 重置时间（击杀时时间重置为满）
            _remainingTime = _comboWindow;
            UpdateProgressBar();

            // 停止警告动画
            if (_isWarningState)
            {
                if (_view != null)
                    _view.StopProgressBarWarningAnimation(_currentTier.tierColor);
                _isWarningState = false;
            }

            // 播放连击数字跳动动画
            if (_view != null)
                _view.PlayComboCountPunchAnimation();
        }

        /// <summary>
        /// 处理档位变化事件（档位跃迁时触发）
        /// </summary>
        private void OnTierChanged(ComboTierChangedEvent evt)
        {
            _currentTier = evt.ComboTier;

            if (_view != null)
            {
                // 更新档位名称和颜色
                _view.SetTierNameText(_currentTier.comboState.ToString());
                _view.SetTierNameColor(_currentTier.tierColor);
                _view.SetComboCountColor(_currentTier.tierColor);
                _view.SetProgressBarColor(_currentTier.tierColor);

                // 播放档位跃迁特效（闪烁 + 缩放）
                _view.PlayTierNameFlashAnimation(Color.white);
            }
        }

        /// <summary>
        /// 处理连击中断事件（超时触发）
        /// </summary>
        private void OnComboExpired(ComboExpiredEvent evt)
        {
            // 连击中断，UI 已在 OnComboChanged(0) 中隐藏
            // 可在此添加额外的中断特效（如屏幕边缘红色闪光）

            _isWarningState = false;
        }

        // ═══════════════════════════════════════════════════════════════
        // Update 方法：每帧更新进度条和警告状态
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 每帧更新（由 Wrapper 调用）
        /// </summary>
        public void OnUpdate()
        {
            if (_currentCombo == 0)
                return;

            // 递减时间
            _remainingTime -= Time.deltaTime;

            // 更新进度条
            UpdateProgressBar();

            // 检查警告状态
            CheckWarningState();
        }

        /// <summary>
        /// 更新时间进度条
        /// </summary>
        private void UpdateProgressBar()
        {
            if (_view == null)
                return;

            float fillAmount = Mathf.Clamp01(_remainingTime / _comboWindow);
            _view.SetProgressBarFill(fillAmount);
        }

        /// <summary>
        /// 检查警告状态（剩余时间低于阈值时闪烁红色）
        /// </summary>
        private void CheckWarningState()
        {
            if (_view == null)
                return;

            float warningThreshold = 1f; // 从 ComboConfigSO 获取
            bool shouldWarn = _remainingTime <= warningThreshold;

            // 状态切换时触发动画
            if (shouldWarn && !_isWarningState)
            {
                _view.PlayProgressBarWarningAnimation(Color.red);
                _isWarningState = true;
            }
            else if (!shouldWarn && _isWarningState)
            {
                _view.StopProgressBarWarningAnimation(_currentTier.tierColor);
                _isWarningState = false;
            }
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// 继承 UILogicBase 以获得自动事件订阅管理能力
    /// </summary>
    public class ComboUIViewLogic : UILogicBase
    {
        private ComboUIViewLogicCore _core = new ComboUIViewLogicCore();
        private ComboUIView _view;

        public override void Bind(UIViewBase view)
        {
            base.Bind(view);
            _view = view as ComboUIView;
            if (_view == null)
            {
                Debug.LogError(
                    $"[UI] ComboUIViewLogic: Bind failed! View is not {typeof(ComboUIView)}"
                );
                return;
            }

            _core.Bind(view);
            Debug.Log("[ComboUI] ComboUIViewLogic 绑定成功");
        }

        public override void OnOpen(UIArgs args)
        {
            base.OnOpen(args);
            _core.OnOpen(args);
            Debug.Log("[ComboUI] ComboUIView 已打开，开始订阅连击事件");
        }

        public override void OnClose()
        {
            _core.OnClose();
            _view = null;
            base.OnClose();
        }

        public override void OnCovered()
        {
            base.OnCovered();
            _core.OnCovered();
        }

        public override void OnResume()
        {
            base.OnResume();
            _core.OnResume();
        }

        /// <summary>
        /// 每帧更新（驱动进度条和警告状态）
        /// </summary>
        void Update()
        {
            _core?.OnUpdate();
        }
    }
}
