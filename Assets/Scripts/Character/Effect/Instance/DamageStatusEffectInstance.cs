using Character;
using Character.Components;
using UnityEngine;

/// <summary>
/// 伤害状态效果实例
/// 支持瞬间伤害和持续伤害两种模式
/// </summary>
public class DamageStatusEffectInstance : StatusEffectInstanceBase, IDamageSourceAware
{
    public override string EffectId => _def.effectId;

    private readonly DamageStatusEffectDefinitionSO _def;
    private float _damageTimer;
    private bool _instantDamageApplied;

    // 伤害来源引用
    private CharacterBase _damageSource;

    public DamageStatusEffectInstance(DamageStatusEffectDefinitionSO def, CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
        _damageTimer = 0f;
        _instantDamageApplied = false;
        if (caster != null) SetDamageSource(caster);
    }

    /// <summary>
    /// 设置伤害来源
    /// </summary>
    public void SetDamageSource(CharacterBase source)
    {
        _damageSource = source;
    }
    
    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        
        // 设置伤害来源为施法者（如果可用）
        if (comp != null && _damageSource == null)
        {
            var characterBase = comp.GetComponent<CharacterBase>();
            if (characterBase != null)
            {
                // 尝试从技能目标上下文获取施法者信息
                // 这里需要通过其他方式传递施法者信息，暂时使用当前角色作为来源
                _damageSource = characterBase;
            }
        }
        
        // 如果是瞬间伤害（damageInterval为0），立即应用伤害
        if (_def.damageInterval <= 0f)
        {
            ApplyInstantDamage();
        }
    }
    
    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);
        
        // 持续伤害逻辑
        if (_def.damageInterval > 0f)
        {
            _damageTimer += deltaTime;
            
            // 检查是否到了造成伤害的时间
            if (_damageTimer >= _def.damageInterval)
            {
                ApplyDamage();
                _damageTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// 应用瞬间伤害
    /// </summary>
    private void ApplyInstantDamage()
    {
        if (_instantDamageApplied) return;
        
        ApplyDamage();
        _instantDamageApplied = true;
        
        // 瞬间伤害效果应用后立即标记为过期
        remainingTime = -1f;
    }
    
    /// <summary>
    /// 应用伤害到目标
    /// </summary>
    private void ApplyDamage()
    {
        if (stats == null) return;
        
        // 计算伤害值
        float damage = CalculateDamage();
        
        // 创建伤害信息
        var damageInfo = CreateDamageInfo(damage);
        
        // 应用伤害
        stats.TakeDamage(damageInfo);
    }
    
    /// <summary>
    /// 计算伤害值
    /// </summary>
    private float CalculateDamage()
    {
        float damage = _def.baseDamage;
        
        // 如果需要基于攻击力计算伤害
        if (_def.useAttackPower && stats != null)
        {
            damage += stats.AttackPower.Value * _def.attackPowerMultiplier;
        }
        
        return damage;
    }
    
    /// <summary>
    /// 创建伤害信息
    /// </summary>
    private DamageInfo CreateDamageInfo(float damage)
    {
        var damageInfo = DamageInfo.Create(damage);
        
        // 设置伤害来源
        if (_def.useCasterAsSource && _damageSource != null)
        {
            damageInfo.Source = _damageSource.gameObject;
        }
        
        return damageInfo;
    }
    
    /// <summary>
    /// 获取伤害类型
    /// </summary>
    public DamageType GetDamageType()
    {
        return _def.damageType;
    }
    
    /// <summary>
    /// 获取伤害间隔
    /// </summary>
    public float GetDamageInterval()
    {
        return _def.damageInterval;
    }
}