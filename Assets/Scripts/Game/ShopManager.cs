using System;
using System.Linq;
using CDTU.Utils;
using UnityEngine;

/// <summary>
/// 商店管理器 - 处理商品购买逻辑
/// </summary>
public class ShopManager : Singleton<ShopManager>
{
    private InventoryManager inventoryManager;

    // 购买成功事件
    public event Action<string, int> OnItemPurchased; // (itemName, cost)
    public event Action<string> OnPurchaseFailed; // (reason)

    public void Initialize(InventoryManager invManager)
    {
        inventoryManager = invManager;
    }

    /// <summary>
    /// 购买生命值恢复
    /// </summary>
    /// <param name="spendCoins">消耗金币数</param>
    /// <param name="healAmount">恢复生命值</param>
    /// <returns>是否购买成功</returns>
    public bool BuyBloods(int spendCoins, int healAmount = 50)
    {

        // 使用原子操作消耗金币
        if (!inventoryManager.SpendCoins(spendCoins))
        {
            Debug.LogWarning("[ShopManager] Not enough coins to buy bloods.");
            OnPurchaseFailed?.Invoke("Not enough coins");
            return false;
        }

        // 获取玩家并恢复生命
        var playerState = PlayerManager.Instance?.GetLocalPlayerData();
        if (playerState?.Controller == null)
        {
            Debug.LogError("[ShopManager] Player not found, refunding coins.");
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("Player not found");
            return false;
        }

        var health = playerState.Controller.GetComponent<HealthComponent>();
        if (health == null)
        {
            Debug.LogError("[ShopManager] Player has no HealthComponent, refunding coins.");
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("Health component missing");
            return false;
        }

        health.Heal(healAmount);
        OnItemPurchased?.Invoke("Bloods", spendCoins);
        return true;
    }

    /// <summary>
    /// 购买随机卡牌
    /// </summary>
    /// <param name="spendCoins">消耗金币数</param>
    /// <returns>是否购买成功</returns>
    public bool BuyCards(int spendCoins)
    {
        if (inventoryManager == null)
        {
            Debug.LogError("[ShopManager] InventoryManager is not initialized.");
            OnPurchaseFailed?.Invoke("System not initialized");
            return false;
        }

        // 使用原子操作消耗金币
        if (!inventoryManager.SpendCoins(spendCoins))
        {
            Debug.LogWarning("[ShopManager] Not enough coins to buy cards.");
            OnPurchaseFailed?.Invoke("Not enough coins");
            return false;
        }

        // 获取随机卡牌
        var cardDatabase = GameRoot.Instance?.CardDatabase;
        if (cardDatabase == null)
        {
            Debug.LogError("[ShopManager] CardDatabase not found, refunding coins.");
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("Card database missing");
            return false;
        }

        string cardId = cardDatabase.GetRandomCardId();
        if (InventoryManager.Instance.ActiveCardIdInfos.Any(info => info.cardId == cardId))
        {
            Debug.LogWarning("[ShopManager] Player already owns the card, refunding coins.");
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("Card already owned");
            return false;
        }
        if (string.IsNullOrEmpty(cardId))
        {
            Debug.LogError("[ShopManager] Failed to get random card, refunding coins.");
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("No cards available");
            return false;
        }

        inventoryManager.AddCardById(cardId);
        OnItemPurchased?.Invoke($"Card:{cardId}", spendCoins);
        return true;
    }
}
