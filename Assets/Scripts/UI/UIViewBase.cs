using System.Collections.Generic;
using UnityEngine;
namespace UI
{
    public abstract class UIViewBase : MonoBehaviour
    {
        // 所有 UI Prefab 都继承此类
        private List<IUILogic> _logics = new List<IUILogic>();

        public void AddLogic(IUILogic logic)
        {
            logic.Bind(this);
            _logics.Add(logic);
        }
        public virtual void OnCreate() { }
        public virtual void OnOpen(UIArgs args)
        {
            foreach (var logic in _logics)
                logic.OnOpen(args);
        }
        public virtual void OnClose()
        {
            foreach (var logic in _logics)
                logic.OnClose();
        }
        public virtual void OnDestroyView() { }

        // 在对象被销毁时清理绑定的逻辑，防止残留引用和内存泄露
        protected virtual void OnDestroy()
        {
            // 尝试安全地调用每个逻辑的关闭逻辑
            if (_logics != null)
            {
                foreach (var logic in _logics)
                {
                    try { logic.OnClose(); } catch { }
                }
                _logics.Clear();
            }

            try
            {
                OnDestroyView();
            }
            catch { }
        }
    }
}