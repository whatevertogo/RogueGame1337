using System;
using Character.Components;
using RogueGame.Events;
using UnityEngine;

/// <summary>
/// 层间奖励系统服务
/// 职责：处理层间过渡时的奖励发放（满血 + 40 金币 + 卡牌选择）
/// 通过订阅 RewardSelectionRequestedEvent 事件触发奖励发放
/// </summary>
public sealed class FloorRewardSystemService
{
    private readonly PlayerManager playerManager;
    private readonly InventoryManager inventoryManager;
    private readonly GameWinLayerRewardConfig gameWinLayerRewardConfig;

    private bool _subscribed = false;

    public FloorRewardSystemService(
        PlayerManager playerManager,
        InventoryManager inventoryManager,
        GameWinLayerRewardConfig gameWinLayerRewardConfig
    )
    {
        this.playerManager = playerManager;
        this.inventoryManager = inventoryManager;
        this.gameWinLayerRewardConfig = gameWinLayerRewardConfig;
    }

    /// <summary>
    /// 订阅事件（在 GameRoot 初始化后调用）
    /// </summary>
    public void Subscribe()
    {
        if (_subscribed) return;

        EventBus.Subscribe<RewardSelectionRequestedEvent>(OnRewardSelectionRequested);
        _subscribed = true;
    }

    /// <summary>
    /// 取消订阅（在游戏关闭或清理时调用）
    /// </summary>
    public void Unsubscribe()
    {
        if (!_subscribed) return;

        EventBus.Unsubscribe<RewardSelectionRequestedEvent>(OnRewardSelectionRequested);
        _subscribed = false;
    }

    /// <summary>
    /// 直接发放层间奖励（不通过事件）
    /// </summary>
    public void GrantLayerRewards(int layer)
    {
        GrantRewardsInternal();
    }

    private void OnRewardSelectionRequested(RewardSelectionRequestedEvent evt)
    {
        GrantRewardsInternal();
    }

    private void GrantRewardsInternal()
    {
        if (inventoryManager == null) return;

        // 1. 满血（如果有 PlayerManager 访问权限）
        if (gameWinLayerRewardConfig.fullHealOnLayerTransition && playerManager != null)
        {
            HealAllPlayers();
        }

        // 2. 给予金币
        inventoryManager.AddCoins(gameWinLayerRewardConfig.layerRewardCoins);

        // 3. 给予随机主动卡（带去重检测）
        // TODO-UI选择有空再说
        var result = inventoryManager.AddRandomActiveCardSmart();

        if (result.ConvertedToCoins)
        {
            Debug.Log($"[FloorRewardSystemService] 层间奖励卡牌重复，已转换为 {result.CoinsAmount} 金币");
        }
        else if (result.Added)
        {
            Debug.Log($"[FloorRewardSystemService] 层间奖励：获得主动卡 '{result.CardId}'");
        }
    }

    /// <summary>
    /// 治疗所有玩家到满血
    /// </summary>
    private void HealAllPlayers()
    {
        if (playerManager == null) return;

        var allPlayers = playerManager.GetAllPlayersData();
        if (allPlayers == null) return;

        foreach (var playerData in allPlayers)
        {
            if (playerData?.Controller == null) continue;

            var stats = playerData.Controller.GetComponent<CharacterStats>();
            if (stats != null)
            {
                // 治疗到满血
                float missingHp = stats.MaxHP.Value - stats.CurrentHP;
                if (missingHp > 0)
                {
                    stats.Heal(missingHp);
                }
            }
        }
    }
}
