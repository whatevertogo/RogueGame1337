using System;
using UnityEngine;
using UI;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// DeadUIView 纯逻辑核心（可单元测试）
    /// </summary>
    public class DeadUIViewLogicCore
    {
        protected DeadUIView _view;
        public virtual void Bind(UIViewBase view)
        {
            _view = view as DeadUIView;
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

        public virtual void OnCovered()
        {
            // 被同层新 UI 覆盖时的默认处理（子类可重写）
        }

        public virtual void OnResume()
        {
            // 从覆盖状态恢复时的默认处理（子类可重写）
        }


        public void OnRetryClicked()
        {
            GameRoot.Instance.GameFlowCoordinator.RestartGame();
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// </summary>
    public class DeadUIViewLogic : MonoBehaviour, IUILogic
    {
        private DeadUIViewLogicCore _core = new DeadUIViewLogicCore();
        private DeadUIView _view;

        public void Bind(UIViewBase view)
        {
            _view = view as DeadUIView;
            if (_view == null)
            {
                CDTU.Utils.Logger.LogError($"[UI] DeadUIViewLogic: Bind failed! View is not {typeof(DeadUIView)}");
                return;
            }

            _core.Bind(view);
            _view.BindRetryButton(OnRetryClicked);
        }

        public void OnOpen(UIArgs args)
        {
            _core.OnOpen(args);
        }

        public void OnClose()
        {
            _core.OnClose();
            if (_view != null) _view.BindRetryButton(null);
            _view = null;
        }

        public void OnCovered()
        {
            _core.OnCovered();
        }

        public void OnResume()
        {
            _core.OnResume();
        }

        private void OnRetryClicked()
        {
            _core.OnRetryClicked();
        }
    }
}
