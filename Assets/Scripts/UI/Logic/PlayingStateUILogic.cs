using System;
using UnityEngine;
using UI;

namespace Game.UI
{
    /// <summary>
    /// PlayingStateUI 纯逻辑核心（可单元测试）
    /// </summary>
    public class PlayingStateUILogicCore
    {
        protected PlayingStateUIView _view;
        public virtual void Bind(UIViewBase view)
        {
            _view = view as PlayingStateUIView;
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
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// </summary>
    public class PlayingStateUILogic : MonoBehaviour, IUILogic
    {
        private PlayingStateUILogicCore _core = new PlayingStateUILogicCore();

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
    }
}
