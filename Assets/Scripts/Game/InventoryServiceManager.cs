using System;
using System.Collections.Generic;
using System.Linq;
using CDTU.Utils;
using RogueGame.Events;
using RogueGame.Game.Service.Inventory;
using RogueGame.Items;

/// <summary>
/// InventoryManager - 协调层
/// 负责协调各服务，提供统一 API
/// </summary>
public sealed class InventoryServiceManager : Singleton<InventoryServiceManager>
{
    // 服务层
    public CoinService CoinService { get; private set; }
    public ActiveCardService ActiveCardService { get; private set; }
    public PassiveCardService PassiveCardService { get; private set; }
    public ActiveCardUpgradeService ActiveCardUpgradeService { get; private set; }
    public ActiveCardEnergyService ActiveCardEnergyService { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        // 初始化服务
        CoinService = new CoinService();
        ActiveCardService = new ActiveCardService();
        PassiveCardService = new PassiveCardService();
        ActiveCardUpgradeService = new ActiveCardUpgradeService(ActiveCardService);
        ActiveCardEnergyService = new ActiveCardEnergyService(ActiveCardService);

        // 转发事件（保持外部订阅兼容）
        CoinService.OnCoinsChanged += coins => OnCoinsChanged?.Invoke(coins);
        ActiveCardService.OnActiveCardInstanceAdded += id => OnActiveCardInstanceAdded?.Invoke(id);
        ActiveCardService.OnActiveCardEnergyChanged += (id, energy) => OnActiveCardEnergyChanged?.Invoke(id, energy);
        ActiveCardService.OnActiveCardEquipChanged += id => OnActiveCardEquipChanged?.Invoke(id);
        ActiveCardUpgradeService.OnCardLevelUp += (id, level) => OnActiveCardLevelUp?.Invoke(id, level);
    }

    #region 对外只读访问（兼容旧 API）

    public int Coins => CoinService.Coins;
    public IReadOnlyList<ActiveCardState> ActiveCardStates => ActiveCardService.ActiveCardStates;
    public IEnumerable<ActiveCardView> ActiveCardViews => ActiveCardService.ActiveCardViews;
    public IReadOnlyList<PassiveCardInfo> PassiveCards => PassiveCardService.Cards;

    #endregion

    #region 事件（兼容旧 API）

    public event Action<int> OnCoinsChanged;
    public event Action<string> OnActiveCardInstanceAdded;
    public event Action<string, int> OnActiveCardEnergyChanged;
    public event Action<string> OnActiveCardEquipChanged;
    public event Action<string, int> OnActiveCardLevelUp;

    #endregion

    #region 金币 API（委托给 CoinService）

    public void AddCoins(int amount) => CoinService.AddCoins(amount);
    public bool SpendCoins(int amount) => CoinService.SpendCoins(amount);
    public void RemoveCoins(int amount) => CoinService.RemoveCoins(amount);
    public void SetCoins(int coins) => CoinService.SetCoins(coins);
    public int CoinsNumber => CoinService.CoinsNumber;

    #endregion

    #region 主动卡 API（委托给 ActiveCardService）

    public string CreateActiveCardInstanceInternal(string cardId, int initialEnergy)
        => ActiveCardService.CreateInstance(cardId, initialEnergy);

    public ActiveCardState GetActiveCard(string instanceId) => ActiveCardService.GetCard(instanceId);
    public ActiveCardState GetActiveCardState(string instanceId) => ActiveCardService.GetCard(instanceId);
    public ActiveCardState GetFirstInstanceByCardId(string cardId) => ActiveCardService.GetFirstByCardId(cardId);

    public void EquipActiveCard(string instanceId, string playerId) => ActiveCardService.EquipCard(instanceId, playerId);
    public void MarkInstanceEquipped(string instanceId, string playerId) => ActiveCardService.EquipCard(instanceId, playerId);

    public void RemoveActiveCardInstance(string instanceId) => ActiveCardService.RemoveInstance(instanceId);
    public bool RemoveActiveCardByCardId(string cardId) => ActiveCardService.RemoveByCardId(cardId);

    public bool HasActiveCard(string cardId) => ActiveCardService.HasCard(cardId);
    public int GetActiveCardCount(string cardId) => ActiveCardService.GetCount(cardId);

    #endregion

    #region 被动卡 API（委托给 PassiveCardService）

    public void AddPassiveCard(string cardId, int count = 1, CardAcquisitionSource source = CardAcquisitionSource.Other)
        => PassiveCardService.AddCard(cardId, count, source);

    public void RemovePassiveCard(string cardId, int count = 1) => PassiveCardService.RemoveCard(cardId, count);
    public int GetPassiveCardCount(string cardId) => PassiveCardService.GetCount(cardId);

    #endregion

    #region 升级 API（委托给 ActiveCardUpgradeService）

    public int GetActiveCardLevel(string cardId) => ActiveCardUpgradeService.GetLevel(cardId);
    public int UpgradeActiveCard(string cardId, int? maxLevel = null) => ActiveCardUpgradeService.UpgradeCard(cardId, maxLevel);

    #endregion

    #region 能量 API（委托给 ActiveCardService）

    public void AddEnergy(string instanceId, int amount) => ActiveCardService.AddEnergy(instanceId, amount);
    public bool ConsumeSkillEnergy(string instanceId, in Character.Player.Skill.Targeting.EnergyCostConfig costConfig) => ActiveCardService.ConsumeSkillEnergy(instanceId, costConfig);
    public void AddChargesForKill(string playerId) => ActiveCardEnergyService.AddChargesForKill(playerId);
    public int GetCurrentEnergy(string instanceId) => ActiveCardService.GetCurrentEnergy(instanceId);
    public int GetMaxEnergy(string instanceId) => ActiveCardService.GetMaxEnergy(instanceId);
    public void ResetEnergyToMax(string instanceId) => ActiveCardService.ResetEnergyToMax(instanceId);

    #endregion

    #region 通用 API

    public void ClearAllCards()
    {
        ActiveCardService.Clear();
        PassiveCardService.Clear();
    }

    public void AddCardById(string cardId)
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var def = db.Resolve(cardId);
        if (def == null) return;

        if (def.CardType == CardType.Active)
        {
            AddActiveCardSmart(cardId, def.activeCardConfig.energyPerKill);
        }
        else
        {
            AddPassiveCard(cardId, 1, CardAcquisitionSource.EnemyDrop);
        }
    }

    public void RemoveCardById(string cardId)
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var def = db.Resolve(cardId);
        if (def == null) return;

        if (def.CardType == CardType.Active)
        {
            RemoveActiveCardByCardId(cardId);
        }
        else
        {
            RemovePassiveCard(cardId, 1);
        }
    }

    #endregion

    #region 智能添加（保留在 InventoryManager）

    public ActiveCardAddResult AddActiveCardSmart(string cardId, int initialCharges)
    {
        var result = new ActiveCardAddResult
        {
            CardId = cardId,
            Success = false,
            Added = false,
            Upgraded = false,
            ConvertedToCoins = false,
            CoinsAmount = 0,
            NewLevel = 0
        };

        var existingCard = ActiveCardService.GetFirstByCardId(cardId);
        if (existingCard != null)
        {
            int oldLevel = existingCard.Level;
            // 尝试升级，但从GameRoot的StatLimitConfig获取最大等级限制
            int newLevel = UpgradeActiveCard(cardId, GameRoot.Instance?.StatLimitConfig?.maxActiveSkillLevel);

            if (newLevel > oldLevel)
            {
                result.Success = true;
                result.Upgraded = true;
                result.NewLevel = newLevel;
                result.InstanceId = existingCard.InstanceId;
                CDLogger.Log($"[InventoryManager] '{cardId}' 升级：Lv{oldLevel} → Lv{newLevel}");
                return result;
            }
            else
            {
                var config = GameRoot.Instance?.ActiveCardDeduplicationConfig;
                if (config != null && config.enableDeduplication)
                {
                    int coins = config.duplicateToCoins;
                    AddCoins(coins);
                    result.Success = true;
                    result.ConvertedToCoins = true;
                    result.CoinsAmount = coins;
                    if (config.showDeduplicationLog)
                    {
                        CDLogger.Log($"[InventoryManager] '{cardId}' 已达最大等级，转换为 {coins} 金币");
                    }
                    return result;
                }
                else
                {
                    result.Success = false;
                    CDLogger.LogWarning($"[InventoryManager] '{cardId}' 已达最大等级且未启用去重");
                    return result;
                }
            }
        }

        string instanceId = ActiveCardService.CreateInstance(cardId, initialCharges);
        result.Success = !string.IsNullOrEmpty(instanceId);
        result.Added = result.Success;
        result.InstanceId = instanceId;
        result.NewLevel = 1;

        if (result.Success)
        {
            CDLogger.Log($"[InventoryManager] 添加新技能 '{cardId}' (Lv1)");
        }

        return result;
    }

    public void AddRandomActiveCard()
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return;

        var cardId = db.GetRandomCardId();
        AddActiveCardSmart(cardId, db.Resolve(cardId).activeCardConfig.energyPerKill);
    }

    public ActiveCardAddResult AddRandomActiveCardSmart()
    {
        var db = GameRoot.Instance?.CardDatabase;
        if (db == null) return new ActiveCardAddResult { Success = false };

        var cardId = db.GetRandomCardId();
        var cardDef = db.Resolve(cardId);
        if (cardDef?.activeCardConfig == null) return new ActiveCardAddResult { Success = false };

        return AddActiveCardSmart(cardId, cardDef.activeCardConfig.energyPerKill);
    }

    #endregion
}
