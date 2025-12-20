using System.Collections.Generic;
using UnityEngine;
using System.Linq;


[CreateAssetMenu(fileName = "New Card Database", menuName = "Card System/Card Database")]
/// <summary>
/// 全局卡牌定义注册表：通过 cardId 查找 CardData(SO)
/// 运行时自动初始化；编辑器下会扫描工程中的所有 CardDefinition 并做唯一性检查。
/// </summary>
public class CardDataBase : ScriptableObject
{
    private readonly Dictionary<string, CardDefinition> _cardid_cardDefinition_Map = new Dictionary<string, CardDefinition>();

    // 缓存初始化标志，避免重复构建字典
    private bool _initialized = false;

    public List<CardDefinition> AllCardDefinitions = new List<CardDefinition>();

    /// <summary>
    /// 初始化映射表（幂等）。在编辑器下可手动触发。
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        foreach (var c in AllCardDefinitions)
        {
            if (c == null || string.IsNullOrEmpty(c.cardId)) continue;
            if (!_cardid_cardDefinition_Map.ContainsKey(c.cardId))
            {
                _cardid_cardDefinition_Map[c.cardId] = c;
            }
            else
            {
                Debug.LogWarning($"[CardDataBase] Duplicate CardDefinition cardId detected: {c.cardId} ({c.name} & {_cardid_cardDefinition_Map[c.cardId].name})");
            }
        }
        _initialized = true;
    }

    public CardDefinition Resolve(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        if (!_initialized) Initialize();
        _cardid_cardDefinition_Map.TryGetValue(cardId, out var data);
        return data;
    }

    public bool TryResolve(string cardId, out CardDefinition data)
    {
        data = Resolve(cardId);
        return data != null;
    }

    public IReadOnlyCollection<CardDefinition> GetAllDefinitions()
    {
        if (!_initialized) Initialize();
        return _cardid_cardDefinition_Map.Values;
    }
}
