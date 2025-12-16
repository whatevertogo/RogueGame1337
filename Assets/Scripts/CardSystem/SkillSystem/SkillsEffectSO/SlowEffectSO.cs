using UnityEngine;
using Character.Effects;
using System;

namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(menuName = "Card System/SkillEffect/SlowEffectSO")]
    public class SlowEffectSO : SkillEffectSO
    {
        public float duration = 2f;
        public float slowPercent = 0.3f;

        public override IStatusEffect CreateEffect()
        {
            return new SlowEffect(duration, slowPercent);
        }
    }
}
