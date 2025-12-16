using System.Collections.Generic;
using Character.Effects;
using UnityEngine;

namespace CardSystem.SkillSystem
{
[CreateAssetMenu(fileName = "NewSkillDefinition", menuName = "Card System/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId;
        public Sprite icon;

        [Header("效果列表")]
        [InlineEditor]
        public List<SkillEffectSO> Effects;

        public void Execute(SkillContext context)
        {
            if (context == null || Effects == null) return;

            // 对所有目标应用所有效果，每次都生成新实例
            foreach (var target in context.Targets)
            {
                if (target == null) continue;
                foreach (var effectSO in Effects)
                {
                    var effect = effectSO?.CreateEffect();
                    if (effect != null)
                        context.ApplyStatusEffect(target, effect);
                }
            }
        }
    }
}