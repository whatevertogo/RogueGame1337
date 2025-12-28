using System.Collections.Generic;
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;

namespace Card.SkillSystem.TargetingSystem
{
    /// <summary>
    /// 目标获取策略抽象基类
    /// 所有目标获取逻辑的基类，定义了统一的接口
    /// </summary>
    public abstract class TargetAcquireSO : ScriptableObject, ITargetAcquireStrategy
    {
        /// <summary>
        /// 获取技能目标
        /// </summary>
        /// <param name="ctx">技能目标上下文</param>
        /// <returns>符合条件的目标列表</returns>
        public abstract List<CharacterBase> Acquire(SkillTargetContext ctx);
    }
}