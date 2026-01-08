using System;
using System.Linq;
using CDTU.Utils;
using RogueGame.Game.Service;
using UnityEngine;

/// <summary>
/// 商店管理器 - 处理商品购买逻辑
/// </summary>
public class ShopManager : Singleton<ShopManager>
{
    private InventoryServiceManager inventoryManager;

    // 购买成功事件
    public event Action<string, int> OnItemPurchased; // (itemName, cost)
    public event Action<string> OnPurchaseFailed; // (reason)

    public void Initialize(InventoryServiceManager invManager)
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
        // 获取玩家并检查是否需要治疗
        var playerState = PlayerManager.Instance?.GetLocalPlayerRuntimeState();
        if (playerState?.Controller == null)
        {
            CDTU.Utils.CDLogger.LogWarning("[ShopManager] Player not found.");
            OnPurchaseFailed?.Invoke("Player not found");
            return false;
        }

        var health = playerState.Controller.GetComponent<HealthComponent>();
        if (health == null)
        {
            CDTU.Utils.CDLogger.LogWarning("[ShopManager] Player has no HealthComponent.");
            OnPurchaseFailed?.Invoke("Health component missing");
            return false;
        }

        // 检查是否满血
        if (health.CurrentHP >= health.MaxHP)
        {
            CDTU.Utils.CDLogger.Log(
                "[ShopManager] Player is already at full health. No purchase needed."
            );
            OnPurchaseFailed?.Invoke("Already at full health");
            return false;
        }

        // 使用原子操作消耗金币
        if (!inventoryManager.SpendCoins(spendCoins))
        {
            CDTU.Utils.CDLogger.LogWarning("[ShopManager] Not enough coins to buy bloods.");
            OnPurchaseFailed?.Invoke("Not enough coins");
            return false;
        }

        // 恢复生命
        health.Heal(healAmount);
        OnItemPurchased?.Invoke("Bloods", spendCoins);
        return true;
    }

    /// <summary>
    /// 购买随机卡牌（符合策划案：重复获得自动升级或转换为金币）
    /// </summary>
    /// <param name="spendCoins">消耗金币数</param>
    /// <returns>是否购买成功</returns>
    public bool BuyCards(int spendCoins)
    {
        if (inventoryManager == null)
        {
            CDTU.Utils.CDLogger.LogError("[ShopManager] InventoryManager is not initialized.");
            OnPurchaseFailed?.Invoke("System not initialized");
            return false;
        }

        // 使用原子操作消耗金币
        if (!inventoryManager.SpendCoins(spendCoins))
        {
            CDTU.Utils.CDLogger.LogWarning("[ShopManager] Not enough coins to buy cards.");
            OnPurchaseFailed?.Invoke("Not enough coins");
            return false;
        }

        // 获取随机卡牌
        var cardDatabase = GameRoot.Instance?.CardDatabase;
        if (cardDatabase == null)
        {
            CDTU.Utils.CDLogger.LogError("[ShopManager] CardDatabase not found, refunding coins.");
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("Card database missing");
            return false;
        }

        string cardId = cardDatabase.GetRandomCardId();
        if (string.IsNullOrEmpty(cardId))
        {
            CDTU.Utils.CDLogger.LogError(
                "[ShopManager] Failed to get random card, refunding coins."
            );
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("No cards available");
            return false;
        }

        // 获取卡牌定义
        var cardDef = cardDatabase.Resolve(cardId);
        if (cardDef == null)
        {
            CDTU.Utils.CDLogger.LogError(
                $"[ShopManager] Failed to resolve card '{cardId}', refunding coins."
            );
            inventoryManager.AddCoins(spendCoins); // 退款
            OnPurchaseFailed?.Invoke("Card definition missing");
            return false;
        }

        // 使用智能添加逻辑：处理重复卡升级和金币转换
        if (cardDef.CardType == CardType.Active)
        {
            // 主动卡：使用 AddActiveCardSmart 处理重复升级和金币转换
            var result = inventoryManager.AddActiveCardSmart(
                cardId,
                cardDef.activeCardConfig.energyPerKill
            );

            if (result.Success)
            {
                string purchaseInfo;
                if (result.Added)
                {
                    purchaseInfo = $"New Card:{cardId} (Lv1)";
                }
                else if (result.Upgraded)
                {
                    purchaseInfo = $"Upgraded:{cardId} to Lv{result.NewLevel}";
                }
                else if (result.ConvertedToCoins)
                {
                    purchaseInfo = $"Max Level:{cardId} → +{result.CoinsAmount} Coins";
                }
                else
                {
                    purchaseInfo = $"Card:{cardId}";
                }

                OnItemPurchased?.Invoke(purchaseInfo, spendCoins);
                return true;
            }
            else
            {
                // 添加失败（不应该发生，因为已经处理了重复和满级情况）
                CDTU.Utils.CDLogger.LogError(
                    $"[ShopManager] Failed to add card '{cardId}', refunding coins."
                );
                inventoryManager.AddCoins(spendCoins); // 退款
                OnPurchaseFailed?.Invoke("Failed to add card");
                return false;
            }
        }
        else
        {
            // 被动卡：直接添加（可无限叠加）
            inventoryManager.AddCardById(cardId);
            OnItemPurchased?.Invoke($"Passive Card:{cardId}", spendCoins);
            return true;
        }
    }
}
