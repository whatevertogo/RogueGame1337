
namespace UI
{

    public abstract class UIArgs { }

    public abstract class UIArgs<TView> : UIArgs where TView : UIViewBase { }
}