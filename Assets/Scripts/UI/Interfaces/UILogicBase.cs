using System;
using System.Collections.Generic;
using Core.Events;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// UI Logic 辅助基类（可选继承）
    /// - 自动管理 EventBus 事件订阅/取消订阅
    /// - 提供便捷的事件订阅 API
    /// - 确保生命周期正确处理
    /// </summary>
    public abstract class UILogicBase : MonoBehaviour, IUILogic
    {
        protected UIViewBase View { get; private set; }

        /// <summary>
        /// 事件订阅记录（用于自动取消订阅）
        /// </summary>
        private readonly List<Action> _unsubscribeActions = new();

        private bool _opened;

        #region IUILogic 实现

        public virtual void Bind(UIViewBase view)
        {
            View = view;
        }

        public virtual void OnOpen(UIArgs args)
        {
            if (_opened) return;
            _opened = true;

            // 子类在此订阅事件
            OnSubscribeEvents();
        }

        public virtual void OnClose()
        {
            if (!_opened) return;
            _opened = false;

            // 自动取消所有订阅
            UnsubscribeAll();
        }

        public virtual void OnCovered()
        {
            // 默认不处理
        }

        public virtual void OnResume()
        {
            // 默认不处理
        }

        #endregion

        #region 事件订阅辅助

        /// <summary>
        /// 子类重写此方法进行事件订阅
        /// </summary>
        protected virtual void OnSubscribeEvents()
        {
        }

        /// <summary>
        /// 订阅事件（会在 OnClose 时自动取消）
        /// </summary>
        protected void Subscribe<T>(Action<T> handler)
        {
            EventBus.Subscribe(handler);
            _unsubscribeActions.Add(() => EventBus.Unsubscribe(handler));
        }

        /// <summary>
        /// 取消所有已订阅的事件
        /// </summary>
        private void UnsubscribeAll()
        {
            foreach (var unsubscribe in _unsubscribeActions)
            {
                try { unsubscribe(); } catch { }
            }
            _unsubscribeActions.Clear();
        }

        #endregion

        #region Unity 生命周期

        protected virtual void OnDestroy()
        {
            // 保险措施：确保取消订阅
            UnsubscribeAll();
        }

        #endregion
    }
}
