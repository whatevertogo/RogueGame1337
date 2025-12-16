using UnityEngine;
using Character.Effects;
using System;

namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(menuName = "Card System/SkillEffect/BurnEffectSO")]
    public class BurnEffectSO : SkillEffectSO
    {
        public float duration = 3f;
        public float damagePerTick = 5f;
        public float tickInterval = 1f;

        public override IStatusEffect CreateEffect()
        {
            return new BurnEffect(duration, damagePerTick, tickInterval);
        }
    }
}
