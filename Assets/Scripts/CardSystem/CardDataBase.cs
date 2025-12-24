using System.Collections.Generic;
using UnityEngine;
using System.Linq;


[CreateAssetMenu(fileName = "New Card Database", menuName = "Card System/Card Database")]
/// <summary>
/// 全局卡牌定义注册表：通过 cardId 查找 CardDefinition(SO)
/// 运行时自动初始化；编辑器下会扫描工程中的所有 CardDefinition 并做唯一性检查。
/// </summary>
public class CardDataBase : ScriptableObject
{
    // 映射表，非序列化
    private readonly Dictionary<string, CardDefinition> _cardid_cardDefinition_Map = new Dictionary<string, CardDefinition>();

    // 缓存初始化标志
    private bool _initialized = false;

    [Header("所有卡牌定义列表")]
    public List<CardDefinition> AllCardDefinitions = new List<CardDefinition>();

    /// <summary>
    /// 初始化映射表（幂等）。可重复调用。
    /// </summary>
    public void Initialize()
    {
        // 清理旧数据，保证幂等性
        _cardid_cardDefinition_Map.Clear();

        if (AllCardDefinitions == null || !AllCardDefinitions.Any())
        {
            _initialized = true;
            return;
        }

        foreach (var c in AllCardDefinitions)
        {
            if (c == null || string.IsNullOrEmpty(c.CardId))
                continue;

            if (!_cardid_cardDefinition_Map.ContainsKey(c.CardId))
            {
                _cardid_cardDefinition_Map[c.CardId] = c;
            }
            else
            {
                CDTU.Utils.Logger.LogWarning($"[CardDataBase] Duplicate CardDefinition cardId detected: {c.CardId} ({c.name} & {_cardid_cardDefinition_Map[c.CardId].name})");
            }
        }

        _initialized = true;
    }

    /// <summary>
    /// 根据 cardId 查找对应卡牌定义
    /// </summary>
    public CardDefinition Resolve(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) {
            CDTU.Utils.Logger.LogWarning($"[CardDataBase] Resolve: cardId is null or empty");
            return null;
        }

        if (!_initialized) Initialize();
        
        if (_cardid_cardDefinition_Map.TryGetValue(cardId, out var data)) {
            return data;
        } else {
            CDTU.Utils.Logger.LogWarning($"[CardDataBase] Resolve: CardId '{cardId}' not found in database");
            return null;
        }
    }

    /// <summary>
    /// 尝试解析卡牌
    /// </summary>
    public bool TryResolve(string cardId, out CardDefinition data)
    {
        data = Resolve(cardId);
        return data != null;
    }

    /// <summary>
    /// 获取所有卡牌定义
    /// </summary>
    public IEnumerable<CardDefinition> GetAllDefinitions()
    {
        if (!_initialized) Initialize();
        return _cardid_cardDefinition_Map.Values;
    }


    public string GetRandomCardId()
    {
        if (AllCardDefinitions == null || !AllCardDefinitions.Any())
        {
            CDTU.Utils.Logger.LogWarning("[CardDataBase] GetRandomCardId: No card definitions available.");
            return null;
        }

        // 过滤 null 和空 ID，防止编辑器操作引入问题
        var validCards = AllCardDefinitions.Where(x => x != null && !string.IsNullOrEmpty(x.CardId)).ToList();
        if (validCards.Count == 0)
        {
            CDTU.Utils.Logger.LogWarning("[CardDataBase] GetRandomCardId: No valid cards available.");
            return null;
        }

        int randomIndex = Random.Range(0, validCards.Count);
        return validCards[randomIndex].CardId;
    }

    /// <summary>
    /// ScriptableObject 生命周期保证，每次加载资源都重置初始化状态
    /// </summary>
    private void OnEnable()
    {
        _initialized = false;
        _cardid_cardDefinition_Map.Clear();
    }
}
