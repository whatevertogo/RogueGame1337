using Character;
using UnityEngine;

[ManagedData("Character")]
[CreateAssetMenu(fileName = "Enemy", menuName = "RogueGame/Character/Enemy")]
public class EnemyConfigSO : ScriptableObject
{
    public int KillEnergy = 5;

    public int CoinMin = 1;
    public int CoinMax = 5;

    public bool isEliteDefault = false;

    public bool isBoss = false;

    public EnemyAiType aiType = EnemyAiType.MeleeAttack;

    [Header("掉落配置")]
    [Range(0f, 1f)]
    [Tooltip("主动卡牌掉落概率")]
    public float PassiveDropChance = 0f;

    [Range(0f, 1f)]
    [Tooltip("被动卡牌掉落概率")]
    public float ActiveDropChance = 0f;
    [Tooltip("主动卡牌掉落ID（可选）")]
    public string[] PassiveCardIds;
    [Tooltip("被动卡牌掉落ID（可选）")]
    public string[] ActiveCardIds;

}