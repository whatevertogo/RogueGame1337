using Character;
using Character.Effects;
using UnityEngine;

/// <summary>
/// 伤害状态效果定义
/// 支持瞬间伤害和持续伤害两种模式
/// </summary>
[CreateAssetMenu(fileName = "DamageStatusEffectDefinition", menuName = "Character/Effects/Damage Status")]
public class DamageStatusEffectDefinitionSO : StatusEffectDefinitionSO
{
    [Header("伤害设置")]
    [Tooltip("基础伤害值")]
    public float baseDamage = 10f;
    
    [Tooltip("伤害类型")]
    public DamageType damageType = DamageType.Physical;
    
    [Tooltip("伤害间隔（秒），用于持续伤害。0表示瞬间伤害")]
    public float damageInterval = 0f;
    
    [Header("伤害来源设置")]
    [Tooltip("是否使用施法者作为伤害来源")]
    public bool useCasterAsSource = true;
    
    [Tooltip("是否基于施法者攻击力计算伤害")]
    public bool useAttackPower = false;
    
    [Tooltip("攻击力倍率")]
    public float attackPowerMultiplier = 1f;

    public override StatusEffectInstanceBase CreateInstance()
    {
        return new DamageStatusEffectInstance(this);
    }
}