
namespace Character
{
    /// <summary>
    /// 阵营类型
    /// </summary>
    public enum TeamType
    {
        Player = 0,
        Enemy = 1,
        Neutral = 2
    }

    /// <summary>
    /// 角色状态
    /// </summary>
    public enum CharacterState
    {
        Idle,
        Moving,
        Attacking,
        Stunned,
        Dead
    }

    /// <summary>
    /// 属性类型
    /// </summary>
    public enum StatType
    {
        MaxHP,
        HPRegen,
        MoveSpeed,
        Acceleration,
        AttackPower,
        AttackSpeed,
        AttackRange,
        // CritChance,
        // CritDamage,
        Armor,
        Dodge
    }

    /// <summary>
    /// 状态效果类型
    /// </summary>
    public enum EffectType
    {
        Buff,       // 增益
        Debuff,     // 减益
        Neutral     // 中性
    }

    /// <summary>
    /// 属性修饰符类型
    /// </summary>
    public enum StatModType
    {
        Flat = 100,         // 固定加成：+10
        PercentAdd = 200,   // 百分比累加：+10% +20% = +30%
        PercentMult = 300   // 百分比独立乘：×1.1 ×1.2
    }

    public enum EnemyAiType
    {
        None,
        MeleeAttack,
        ProjectileAttack,
    }

}