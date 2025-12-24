using System;
using System.Collections.Generic;
using Character;
using Character.Components;
using Character.Effects;
using Character.Player;
using RogueGame.Events;
using UnityEngine;

/// <summary>
/// 被动卡牌效果应用服务
/// 职责：监听被动卡牌拾取事件，自动应用被动效果到玩家
/// </summary>
public sealed class PassiveCardApplicationService
{
    private readonly InventoryManager inventoryManager;
    private readonly PlayerManager playerManager;
    private readonly CardDataBase cardDatabase;
    private readonly EffectFactory effectFactory;

    // 追踪已应用的被动效果 (cardId -> List<effect instances>)
    private readonly Dictionary<string, List<IStatusEffect>> _appliedEffects = new();

    private bool _subscribed = false;

    public PassiveCardApplicationService(
        InventoryManager inventoryManager,
        PlayerManager playerManager,
        CardDataBase cardDatabase
    )
    {
        this.inventoryManager = inventoryManager;
        this.playerManager = playerManager;
        this.cardDatabase = cardDatabase;
        this.effectFactory = new EffectFactory();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    public void Subscribe()
    {
        if (_subscribed) return;

        EventBus.Subscribe<PassiveCardAcquiredEvent>(OnPassiveCardAcquired);
        EventBus.Subscribe<PassiveCardRemovedEvent>(OnPassiveCardRemoved);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Subscribe<EntityKilledEvent>(OnEntityKilled); // 条件触发卡：P08 吸血体质
        EventBus.Subscribe<PlayerSkillCastEvent>(OnPlayerSkillCast); // 条件触发卡：P10 暴风骤雨
        _subscribed = true;

        CDTU.Utils.CDLogger.Log("[PassiveCardApplicationService] 已订阅被动卡牌事件");
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe()
    {
        if (!_subscribed) return;

        EventBus.Unsubscribe<PassiveCardAcquiredEvent>(OnPassiveCardAcquired);
        EventBus.Unsubscribe<PassiveCardRemovedEvent>(OnPassiveCardRemoved);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Unsubscribe<EntityKilledEvent>(OnEntityKilled);
        EventBus.Unsubscribe<PlayerSkillCastEvent>(OnPlayerSkillCast);
        _subscribed = false;
    }

    /// <summary>
    /// 被动卡牌拾取事件处理
    /// </summary>
    private void OnPassiveCardAcquired(PassiveCardAcquiredEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.CardId))
        {
            CDTU.Utils.CDLogger.LogWarning("[PassiveCardApplicationService] 无效的被动卡牌拾取事件");
            return;
        }

        // 解析卡牌定义
        var cardDef = cardDatabase.Resolve(evt.CardId);
        if (cardDef == null)
        {
            CDTU.Utils.CDLogger.LogError($"[PassiveCardApplicationService] 无法找到卡牌定义: {evt.CardId}");
            return;
        }

        if (cardDef.passiveCardConfig == null)
        {
            CDTU.Utils.CDLogger.LogError($"[PassiveCardApplicationService] 卡牌 {evt.CardId} 没有被动卡配置");
            return;
        }

        // 获取本地玩家
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null)
        {
            CDTU.Utils.CDLogger.LogWarning("[PassiveCardApplicationService] 没有本地玩家");
            return;
        }

        var statusEffectComponent = localPlayer.GetComponent<StatusEffectComponent>();
        if (statusEffectComponent == null)
        {
            CDTU.Utils.CDLogger.LogError("[PassiveCardApplicationService] 本地玩家没有 StatusEffectComponent");
            return;
        }

        // 应用被动效果
        ApplyPassiveEffects(cardDef.CardId, cardDef.passiveCardConfig, statusEffectComponent, evt.Count);
    }

    /// <summary>
    /// 被动卡牌移除事件处理
    /// </summary>
    private void OnPassiveCardRemoved(PassiveCardRemovedEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.CardId))
        {
            CDTU.Utils.CDLogger.LogWarning("[PassiveCardApplicationService] 无效的被动卡牌移除事件");
            return;
        }

        // 获取本地玩家
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        var statusEffectComponent = localPlayer.GetComponent<StatusEffectComponent>();
        if (statusEffectComponent == null) return;

        // 移除被动效果
        RemovePassiveEffects(evt.CardId, statusEffectComponent, evt.Count);
    }

    /// <summary>
    /// 玩家重生事件处理（重新应用所有被动效果）
    /// </summary>
    private void OnPlayerRespawn(PlayerRespawnEvent evt)
    {
        CDTU.Utils.CDLogger.Log("[PassiveCardApplicationService] 玩家重生，重新应用被动效果");

        // 清除已追踪的效果
        _appliedEffects.Clear();

        // 获取本地玩家
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        var statusEffectComponent = localPlayer.GetComponent<StatusEffectComponent>();
        if (statusEffectComponent == null) return;

        // 重新应用所有当前持有的被动卡
        var passiveCards = inventoryManager.PassiveCards;
        foreach (var passiveCard in passiveCards)
        {
            var cardDef = cardDatabase.Resolve(passiveCard.CardId);
            if (cardDef?.passiveCardConfig != null)
            {
                ApplyPassiveEffects(passiveCard.CardId, cardDef.passiveCardConfig,
                    statusEffectComponent, passiveCard.Count);
            }
        }
    }

    /// <summary>
    /// 应用被动效果
    /// </summary>
    private void ApplyPassiveEffects(
        string cardId,
        PassiveCardConfig config,
        StatusEffectComponent target,
        int count
    )
    {
        if (config.passiveEffects == null || config.passiveEffects.Length == 0)
        {
            CDTU.Utils.CDLogger.LogWarning($"[PassiveCardApplicationService] 卡牌 {cardId} 没有配置被动效果");
            return;
        }

        // 如果还没追踪这个卡牌的效果，创建列表
        if (!_appliedEffects.ContainsKey(cardId))
        {
            _appliedEffects[cardId] = new List<IStatusEffect>();
        }

        // 获取玩家作为施法者
        var caster = GetLocalPlayer();

        // 应用每个效果
        for (int i = 0; i < count; i++)
        {
            foreach (var effectDef in config.passiveEffects)
            {
                if (effectDef == null) continue;

                // 创建效果实例
                var effectInstance = effectFactory.CreateInstance(effectDef, caster);

                // 设置为永久效果（持续时间 = -1 表示永久）
                if (effectInstance is StatusEffectInstanceBase baseInstance)
                {
                    // 被动卡效果通常是永久的，除非配置中指定了持续时间
                    // 这里我们保持原配置的持续时间
                }

                // 应用到目标
                target.AddEffect(effectInstance);

                // 追踪已应用的效果
                _appliedEffects[cardId].Add(effectInstance);
            }
        }
    }

    /// <summary>
    /// 移除被动效果
    /// </summary>
    private void RemovePassiveEffects(
        string cardId,
        StatusEffectComponent target,
        int count
    )
    {
        if (!_appliedEffects.ContainsKey(cardId))
        {
            CDTU.Utils.CDLogger.LogWarning($"[PassiveCardApplicationService] 卡牌 {cardId} 没有已应用的效果");
            return;
        }

        var effects = _appliedEffects[cardId];
        int removedCount = 0;

        // 移除指定数量的效果（从后往前）
        for (int i = effects.Count - 1; i >= 0 && removedCount < count; i--)
        {
            var effect = effects[i];
            target.RemoveEffect(effect);
            effects.RemoveAt(i);
            removedCount++;
        }

        CDTU.Utils.CDLogger.Log($"[PassiveCardApplicationService] 移除了 {removedCount} 个效果 from {cardId}");

        // 如果该卡牌没有效果了，移除条目
        if (effects.Count == 0)
        {
            _appliedEffects.Remove(cardId);
        }
    }

    /// <summary>
    /// 获取本地玩家
    /// </summary>
    private CharacterBase GetLocalPlayer()
    {
        var players = playerManager.GetAllPlayersData();
        foreach (var player in players)
        {
            if (player.IsLocal && player.Controller != null)
            {
                return player.Controller;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取已应用的效果统计（用于调试）
    /// </summary>
    public Dictionary<string, int> GetAppliedEffectsCount()
    {
        var result = new Dictionary<string, int>();
        foreach (var kvp in _appliedEffects)
        {
            result[kvp.Key] = kvp.Value.Count;
        }
        return result;
    }

    #region 条件触发卡牌逻辑

    /// <summary>
    /// 敌人被击杀事件处理（P08 吸血体质：击杀回血）
    /// </summary>
    private void OnEntityKilled(EntityKilledEvent evt)
    {
        if (evt == null || evt.Attacker == null) return;

        // 检查攻击者是否是本地玩家
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null || evt.Attacker != localPlayer.gameObject) return;

        // 检查是否有"吸血体质"卡牌（P08）
        var cardCount = inventoryManager.GetPassiveCardCount("P08");
        if (cardCount <= 0) return;

        // 获取玩家 Stats 组件
        var stats = localPlayer.GetComponent<CharacterStats>();
        if (stats == null) return;

        // 每张卡回复 5 点生命
        float healAmount = 5f * cardCount;
        stats.Heal(healAmount);

        CDTU.Utils.CDLogger.Log($"[PassiveCardApplicationService] 吸血体质触发：回复 {healAmount} 点生命（{cardCount} 张卡）");
    }

    /// <summary>
    /// 玩家技能释放事件处理（P10 暴风骤雨：释放技能后攻速提升）
    /// </summary>
    private void OnPlayerSkillCast(PlayerSkillCastEvent evt)
    {
        if (evt == null) return;

        // 检查是否有"暴风骤雨"卡牌（P10）
        var cardCount = inventoryManager.GetPassiveCardCount("P10");
        if (cardCount <= 0) return;

        // 获取本地玩家
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        var statusEffectComponent = localPlayer.GetComponent<StatusEffectComponent>();
        if (statusEffectComponent == null) return;

        // 获取 P10 卡牌定义，从中读取 StatusEffect 配置
        var cardDef = cardDatabase.Resolve("P10");
        if (cardDef == null || cardDef.passiveCardConfig?.passiveEffects == null || cardDef.passiveCardConfig.passiveEffects.Length == 0)
        {
            CDTU.Utils.CDLogger.LogWarning("[PassiveCardApplicationService] P10 卡牌配置错误，未找到 StatusEffect");
            return;
        }

        // 创建攻速增益效果实例
        var effectDef = cardDef.passiveCardConfig.passiveEffects[0];
        var effectInstance = effectFactory.CreateInstance(effectDef, localPlayer);

        // 设置持续时间为3秒（临时 Buff）
        if (effectInstance is StatusEffectInstanceBase baseInstance)
        {
            baseInstance.SetDuration(3f);
        }

        statusEffectComponent.AddEffect(effectInstance);

        CDTU.Utils.CDLogger.Log($"[PassiveCardApplicationService] 暴风骤雨触发：攻速提升，持续3秒（{cardCount} 张卡）");
    }

    #endregion
}
