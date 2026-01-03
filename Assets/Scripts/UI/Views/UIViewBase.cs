using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 所有 UI Prefab 的基类
    /// - 统一生命周期
    /// - 管理 IUILogic
    /// - 提供覆盖 / 恢复 / 回退语义
    /// - 自动管理 Button 事件清理
    /// </summary>
    public abstract class UIViewBase : MonoBehaviour
    {
        private readonly List<IUILogic> _logics = new();

        /// <summary>
        /// 注册的 Button 事件（用于自动清理）
        /// </summary>
        private readonly List<(Button button, UnityEngine.Events.UnityAction action)> _buttonBindings = new();

        private bool _opened;
        private bool _destroyed;


        /// <summary>
        /// 是否为独占 UI（打开时关闭同层其他 UI）
        /// </summary>
        public abstract bool Exclusive { get; }

        /// <summary>
        /// 是否允许 ESC / Back 回退
        /// </summary>
        public abstract bool CanBack { get; }


        #region Logic 管理

        public void AddLogic(IUILogic logic)
        {
            if (logic == null)
                return;

            logic.Bind(this);
            _logics.Add(logic);
        }

        #endregion

        #region Button 事件管理（自动清理）

        /// <summary>
        /// 安全绑定 Button 点击事件（会在 OnClose 时自动清理）
        /// </summary>
        protected void BindButton(Button button, Action onClick)
        {
            if (button == null || onClick == null) return;

            UnityEngine.Events.UnityAction action = () => onClick();
            button.onClick.AddListener(action);
            _buttonBindings.Add((button, action));
        }

        /// <summary>
        /// 清理所有已绑定的 Button 事件
        /// </summary>
        private void ClearAllButtonBindings()
        {
            foreach (var (button, action) in _buttonBindings)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(action);
                }
            }
            _buttonBindings.Clear();
        }

        #endregion

        #region 生命周期（由 UIManager 驱动）

        /// <summary>
        /// 仅在实例化后调用一次
        /// </summary>
        public virtual void OnCreate()
        {
        }

        /// <summary>
        /// View 入栈 / 显示
        /// </summary>
        public virtual void OnOpen(UIArgs args)
        {
            _opened = true;

            foreach (var logic in _logics)
            {
                try { logic.OnOpen(args); }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        /// <summary>
        /// View 出栈 / 关闭
        /// </summary>
        public virtual void OnClose()
        {
            if (!_opened)
                return;

            _opened = false;

            // 清理 Button 事件
            ClearAllButtonBindings();

            foreach (var logic in _logics)
            {
                try { logic.OnClose(); }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        #endregion

        #region 覆盖 / 恢复（UI 栈语义）

        /// <summary>
        /// 被同层新 UI 覆盖
        /// </summary>
        public virtual void OnCovered()
        {
            // 默认行为：通知逻辑层被覆盖（可重写以定制行为）
            foreach (var logic in _logics)
            {
                try { logic.OnCovered(); } catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        /// <summary>
        /// 从覆盖状态恢复到栈顶
        /// </summary>
        public virtual void OnResume()
        {
            // 默认行为：通知逻辑层恢复（可重写以定制行为）
            foreach (var logic in _logics)
            {
                try { logic.OnResume(); } catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        #endregion

        #region 销毁清理

        /// <summary>
        /// 仅在 GameObject Destroy 时调用
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_destroyed)
                return;

            _destroyed = true;

            // 清理 Button 事件
            ClearAllButtonBindings();

            // 确保逻辑被正确关闭
            if (_opened)
            {
                foreach (var logic in _logics)
                {
                    try { logic.OnClose(); } catch { }
                }
            }

            _logics.Clear();

            try
            {
                OnDestroyView();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>
        /// 子类销毁回调（释放资源 / 注销事件）
        /// </summary>
        protected virtual void OnDestroyView()
        {
        }

        #endregion
    }
}
