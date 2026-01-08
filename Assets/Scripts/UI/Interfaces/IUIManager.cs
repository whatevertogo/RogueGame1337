
namespace UI
{
    public interface IUIManager
    {
        T Open<T>(UIArgs args = null, UILayer layer = UILayer.Normal, params IUILogic[] logics)
            where T : UIViewBase;

        void Close<T>() where T : UIViewBase;

        bool IsOpen<T>() where T : UIViewBase;
    }
}