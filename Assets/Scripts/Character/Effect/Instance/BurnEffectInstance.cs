
using Character.Effects;
using UnityEngine;
using Character.Core;
public class BurnEffectInstance : StatusEffectInstanceBase
{
    public override string EffectId => def.effectId;
    
    private readonly BurnEffectDefinitionSO def;

    public BurnEffectInstance(BurnEffectDefinitionSO def)
        : base(def.duration, def.isStackable)
    {
        this.def = def;
    }


    public override void OnTick(float dt)
    {
        base.OnTick(dt);
        // CharacterStats 没有接受 float 的 TakeDamage 重载，使用 DamageInfo.Create 来传递浮点伤害
        stats.TakeDamage(DamageInfo.Create(def.damagePerSecond * dt));
    }
}