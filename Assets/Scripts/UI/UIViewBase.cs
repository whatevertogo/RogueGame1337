using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 所有 UI Prefab 的基类
    /// - 统一生命周期
    /// - 管理 IUILogic
    /// - 提供覆盖 / 恢复 / 回退语义
    /// </summary>
    public abstract class UIViewBase : MonoBehaviour
    {
        private readonly List<IUILogic> _logics = new();

        private bool _opened;
        private bool _destroyed;


        /// <summary>
        /// 是否为独占 UI（打开时关闭同层其他 UI）
        /// </summary>
        public abstract bool Exclusive { get; }

        /// <summary>
        /// 是否允许 ESC / Back 回退
        /// </summary>
        public virtual bool CanBack => true;

        #region Logic 管理

        public void AddLogic(IUILogic logic)
        {
            if (logic == null)
                return;

            logic.Bind(this);
            _logics.Add(logic);
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
                catch (System.Exception e)
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

            foreach (var logic in _logics)
            {
                try { logic.OnClose(); }
                catch (System.Exception e)
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
            // 默认行为：关闭交互
        }

        /// <summary>
        /// 从覆盖状态恢复到栈顶
        /// </summary>
        public virtual void OnResume()
        {
            // 默认行为：恢复交互
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
            catch (System.Exception e)
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
