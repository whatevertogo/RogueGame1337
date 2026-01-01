namespace UI
{
    public interface IUILogic
    {
        void Bind(UIViewBase view); // 绑定 View
        void OnOpen(UIArgs args);   // UI 打开时
        void OnClose();             // UI 关闭时
        void OnCovered();           // 被同层新 UI 覆盖
        void OnResume();            // 从覆盖状态恢复

    }
}