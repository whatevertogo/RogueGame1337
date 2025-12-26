using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.UI;
using UI;
using UnityEngine;

public enum ShopType
{
    BloodShop,
    CardShop,
}



public class Shop : MonoBehaviour
{

    public ShopType shopType = ShopType.BloodShop;
    [SerializeField] private int Cost = 10;


    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Enter Shop Area");
            _ = OpenShopUI();
        }
    }

    public async Task OpenShopUI()
    {
       await UIManager.Instance.Open<ShopUIView>(new ShopUIArgs($" {shopType}: Cost: {Cost}", shopType, Cost), UILayer.Normal);
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Exit Shop Area");
            UIManager.Instance.Close<ShopUIView>();
        }
    }

}
