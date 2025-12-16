using UnityEngine;
using Character.Effects;
using System;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// 技能效果SO基类，负责生成具体IStatusEffect实例
    /// </summary>
    [Serializable]
    public abstract class SkillEffectSO : ScriptableObject
    {
        public abstract IStatusEffect CreateEffect();
    }
}
