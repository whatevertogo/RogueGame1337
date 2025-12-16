using UnityEngine;

/// <summary>
/// 角色属性配置模板
/// </summary>
[CreateAssetMenu(fileName = "CharacterStats", menuName = "RogueGame/Character Stats")]
public class CharacterStatsSO : ScriptableObject
{
    [Header("基础信息")]
    public string characterName = "Unknown";
    public Sprite icon;

    [Header("生命")]
    [Min(1)] public float maxHP = 100f;
    [Min(0)] public float hpRegen = 0f;

    [Header("移动")]
    [Min(0)] public float moveSpeed = 4f;
    [Min(0)] public float acceleration = 10f;

    [Header("攻击")]
    [Min(0)] public float attackPower = 10f;
    [Min(0.1f)] public float attackSpeed = 1f;
    [Min(0)] public float attackRange = 1.5f;

    // [Header("暴击")]
    // [Range(0, 1)] public float critChance = 0.05f;
    // [Min(1)] public float critDamage = 2f;

    [Header("防御")]
    [Min(0)] public float armor = 0f;
    [Range(0, 1)] public float dodge = 0f;

    [Header("技能冷却速率")]
    [Min(0)] public float skillCooldownReductionRate = 0f;
}