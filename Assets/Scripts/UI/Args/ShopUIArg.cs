
using Game.UI;
using UI;

public class ShopUIArg : UIArgs<ShopUIView>
{

    public int SpendCoins { get; private set; }
    public string ShopMessage { get; private set; }
    public ShopType ShopType { get; private set; }

    public ShopUIArg(string shopMessage, ShopType shopType, int spendCoins)
    {
        ShopMessage = shopMessage;
        ShopType = shopType;
        SpendCoins = spendCoins;
    }

}