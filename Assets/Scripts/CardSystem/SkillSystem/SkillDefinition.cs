using System.Collections.Generic;
using Character;
using Character.Components;
using Character.Effects;
using CardSystem.SkillSystem.Enum;
using UnityEngine;


namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Card System/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId;
        public Sprite icon;

        
        [Tooltip("冷却时间（秒）")]
        public float cooldown = 1f;
        [Tooltip("若大于0，技能触发后延迟检测目标并在延迟结束时应用效果（秒）")]
        public float detectionDelay = 0f;
        public GameObject vfxPrefab;  // 可选

        [Header("Executor")]
        [Tooltip("为该技能指定执行器（AOE / Buff / Projectile 等）")]
        [InlineEditor]
        public SkillExecutorSO executor;

        [Header("效果列表 (legacy)")]
        [Tooltip("使用 StatusEffectDefinitionSO（ScriptableObject）来配置技能要应用的效果。运行时会从 Definition 创建实例。")]
        public List<StatusEffectDefinitionSO> Effects;


       
    }
}
