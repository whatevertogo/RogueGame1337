using System;
using UnityEngine;
using UI;

namespace Game.UI
{
    /// <summary>
    /// BagView 纯逻辑核心（可单元测试）
    /// </summary>
    public class BagViewLogicCore
    {
        protected BagViewView _view;
        public virtual void Bind(UIViewBase view)
        {
            _view = view as BagViewView;
        }

        public virtual void OnOpen(UIArgs args)
        {
            // 在此实现打开时的业务逻辑（纯 C#，易于单元测试）
        }

        public virtual void OnClose()
        {
            // 关闭时清理
            _view = null;
        }

        public void OnPlayerStats1Clicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
        }

        public void OnPlayerStats2Clicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// </summary>
    public class BagViewLogic : MonoBehaviour, IUILogic
    {
        private BagViewLogicCore _core = new BagViewLogicCore();

        public void Bind(UIViewBase view)
        {
            _core.Bind(view);
        }

        public void OnOpen(UIArgs args)
        {
            _core.OnOpen(args);
        }

        public void OnClose()
        {
            _core.OnClose();
        }

        private void OnPlayerStats1Clicked()
        {
            _core.OnPlayerStats1Clicked();
        }

        private void OnPlayerStats12Clicked()
        {
            _core.OnPlayerStats2Clicked();
        }
    }
}
