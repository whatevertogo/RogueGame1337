using System;
using Character;

[Serializable]
public class EnemyConfigStats
{
    public int KillEnergy;

    public int CoinMin;
    public int CoinMax;
    public bool isEliteDefault;
    public bool isBoss;
    public EnemyAiType aiType;

    public float PassiveDropChance;
    public float ActiveDropChance;
    public string PassiveCardId;
    public string ActiveCardId;
    public EnemyConfigStats(EnemyConfigSO configSO)
    {
        KillEnergy = configSO.KillEnergy;
        CoinMin = configSO.CoinMin;
        CoinMax = configSO.CoinMax;
        isEliteDefault = configSO.isEliteDefault;
        isBoss = configSO.isBoss;
        aiType = configSO.aiType;
        PassiveDropChance = configSO.PassiveDropChance;
        ActiveDropChance = configSO.ActiveDropChance;
        PassiveCardId = configSO.PassiveCardId;
        ActiveCardId = configSO.ActiveCardId;
    }


    public EnemyConfigStats(int killEnergy = 5, int coinMin = 1, int coinMax = 3, bool isEliteDefault = false, bool isBoss = false, EnemyAiType aiType = EnemyAiType.MeleeAttack)
    {
        KillEnergy = killEnergy;
        CoinMin = coinMin;
        CoinMax = coinMax;
        this.isEliteDefault = isEliteDefault;
        this.isBoss = isBoss;
        this.aiType = aiType;
    }
}