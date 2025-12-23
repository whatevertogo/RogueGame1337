
using Game.UI;
using UI;

public class ShopUIArgs : UIArgs<ShopUIView>
{

    public int SpendCoins { get; private set; }
    public string ShopMessage { get; private set; }
    public ShopType ShopType { get; private set; }

    public ShopUIArgs(string shopMessage, ShopType shopType, int spendCoins)
    {
        ShopMessage = shopMessage;
        ShopType = shopType;
        SpendCoins = spendCoins;
    }

}