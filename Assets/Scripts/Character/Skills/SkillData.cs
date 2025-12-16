using UnityEngine;
using Character.Core;

namespace Character.Skills
{
    /// <summary>
    /// 技能数据基类
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "RogueGame/Skills/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("基础信息")]
        public string skillName;
        public string description;
        public Sprite icon;
        
        [Header("技能配置")]
        public SkillType skillType;
        public int maxEnergy = 100;
        public bool canBeUsedInRoom = true;
        
        [Header("效果配置")]
        public float effectValue; // 根据技能类型解释不同含义
        public float duration; // 持续效果的时间
        
        public virtual void ApplyEffect(GameObject caster)
        {
            // 子类重写具体效果
        }
        
        public virtual string GetDescription()
        {
            return description;
        }
    }
    
    public enum SkillType
    {
        Healing,        // 治疗
        Dash,           // 冲刺
        FireBurst,      // 火焰爆发
        FrostNova,      // 冰霜新星
        SplitShot,      // 分裂射击
        Shield,         // 护盾
        Berserk,        // 狂暴
        TimeSlow        // 时间减速
    }
}