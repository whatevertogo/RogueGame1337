

using Game.UI;
using UI;

public class DeadUIViewArgs : UIArgs<DeadUIView>
{
    public string Message { get; private set; }

    public DeadUIViewArgs(string message)
    {
        Message = message;
    }
}