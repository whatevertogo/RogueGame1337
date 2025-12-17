using System.Collections.Generic;
using Character.Effects;
using UnityEngine;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "NewSkillDefinition", menuName = "Card System/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId;
        public Sprite icon;
        public float cooldown = 1f;
        [Tooltip("若大于0，技能触发后延迟检测目标并在延迟结束时应用效果（秒）")]
        public float detectionDelay = 0f;
        public SkillTargetingMode targetingMode = SkillTargetingMode.Self;
        public TargetTeam targetTeam = TargetTeam.Hostile;
        public float range = 5f;      // 对单体/投射可用
        public float radius = 3f;     // AOE 半径
        public LayerMask targetMask;  // 哪些 layer 会被检测到
        public GameObject vfxPrefab;  // 可选

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