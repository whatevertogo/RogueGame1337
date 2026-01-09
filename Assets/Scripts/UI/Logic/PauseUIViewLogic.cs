using System;
using UnityEngine;
using UI;

namespace Game.UI
{
    /// <summary>
    /// PauseUIView 纯逻辑核心（可单元测试）
    /// </summary>
    public class PauseUIViewLogicCore
    {
        protected PauseUIView _view;
        public virtual void Bind(UIViewBase view)
        {
            _view = view as PauseUIView;
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

        public void OnSettingClicked()
        {
            Debug.Log("[PauseUI] 设置按钮点击");

            // 切换到设置视图
            if (_view != null && _view.bookInsideUIController != null)
            {
                _view.bookInsideUIController.SwitchState(BookUIState.SettingView);
            }
        }

        public void OnSaveClicked()
        {
            Debug.Log("[PauseUI] 存档按钮点击");

            // 切换到存档视图
            if (_view != null && _view.bookInsideUIController != null)
            {
                _view.bookInsideUIController.SwitchState(BookUIState.SaveView);
            }
        }

        public void OnQuitClicked()
        {
            Debug.Log("[PauseUI] 退出按钮点击");

            // 切换到退出确认视图
            if (_view != null && _view.bookInsideUIController != null)
            {
                _view.bookInsideUIController.SwitchState(BookUIState.QuitView);
            }
        }

        public void OnSaveSlotButton1Clicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
            // 可在此调用 _view.SetXXX 方法更新文本内容
        }

        public void OnSaveSlotButton2Clicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
            // 可在此调用 _view.SetXXX 方法更新文本内容
        }

        public void OnSaveSlotButton3Clicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
            // 可在此调用 _view.SetXXX 方法更新文本内容
        }

        public void OnSaveSlotClicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
            // 可在此调用 _view.SetXXX 方法更新文本内容
        }

        public void OnQuitButton1Clicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// 继承 UILogicBase 以获得自动事件订阅管理能力
    /// </summary>
    public class PauseUIViewLogic : UILogicBase
    {
        private PauseUIViewLogicCore _core = new PauseUIViewLogicCore();
        private PauseUIView _view;

        public override void Bind(UIViewBase view)
        {
            base.Bind(view);
            _view = view as PauseUIView;
            if (_view == null)
            {
                Debug.LogError($"[UI] PauseUIViewLogic: Bind failed! View is not {typeof(PauseUIView)}");
                return;
            }

            _core.Bind(view);
            // Auto-bind event for setting
            _view.BindSettingButton(OnSettingClicked);
            // Auto-bind event for save
            _view.BindSaveButton(OnSaveClicked);
            // Auto-bind event for quit
            _view.BindQuitButton(OnQuitClicked);
            // Auto-bind event for saveSlotButton1Button
            _view.BindSaveSlotButton1Button(OnSaveSlotButton1Clicked);
            // Auto-bind event for saveSlotButton2Button
            _view.BindSaveSlotButton2Button(OnSaveSlotButton2Clicked);
            // Auto-bind event for saveSlotButton3Button
            _view.BindSaveSlotButton3Button(OnSaveSlotButton3Clicked);
            // Auto-bind event for saveSlot
            _view.BindSaveSlotButton(OnSaveSlotClicked);
            // Auto-bind event for quitButton1
            _view.BindQuitButton1Button(OnQuitButton1Clicked);
        }

        public override void OnOpen(UIArgs args)
        {
            base.OnOpen(args);
            _core.OnOpen(args);
        }

        public override void OnClose()
        {
            _core.OnClose();
            // Button 事件由 UIViewBase.BindButton 自动清理，无需手动解绑
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

        private void OnSettingClicked()
        {
            _core.OnSettingClicked();
        }

        private void OnSaveClicked()
        {
            _core.OnSaveClicked();
        }

        private void OnQuitClicked()
        {
            _core.OnQuitClicked();
        }

        private void OnSaveSlotButton1Clicked()
        {
            _core.OnSaveSlotButton1Clicked();
        }

        private void OnSaveSlotButton2Clicked()
        {
            _core.OnSaveSlotButton2Clicked();
        }

        private void OnSaveSlotButton3Clicked()
        {
            _core.OnSaveSlotButton3Clicked();
        }

        private void OnSaveSlotClicked()
        {
            _core.OnSaveSlotClicked();
        }

        private void OnQuitButton1Clicked()
        {
            _core.OnQuitButton1Clicked();
        }
    }
}
