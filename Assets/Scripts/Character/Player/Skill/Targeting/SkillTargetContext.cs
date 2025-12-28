using System.Collections.Generic;
using Character;
using UnityEngine;

namespace Character.Player.Skill.Targeting
{
    /// <summary>
    /// 技能目标上下文：包含施法者、目标和技能参数
    /// </summary>
    public struct SkillTargetContext
    {
        public CharacterBase Caster;
        public Vector3 AimPoint;

        /// <summary>
        /// 瞄准方向（归一化向量）
        /// 对于射线、锥形等方向性技能很重要
        /// </summary>
        public Vector3 AimDirection;

        /// <summary>
        /// 技能强化系数（默认 1.0）
        /// 用于技能升级、Buff 等导致的伤害/效果倍率调整
        /// </summary>
        public float PowerMultiplier;

        // ========== 修改器影响字段 ==========
        /// <summary>
        /// 固定伤害加值（由修改器添加）
        /// </summary>
        public float FlatDamage;

        /// <summary>
        /// 是否为真实伤害（无视防御）
        /// </summary>
        public bool IsTrueDamage;

        /// <summary>
        /// 弹射次数（由修改器修改）
        /// </summary>
        public int BounceCount;

        /// <summary>
        /// 弹射范围（由修改器修改）
        /// </summary>
        public float BounceRange;

        /// <summary>
        /// 穿透数量（由修改器修改）
        /// </summary>
        public int PiercingCount;

        // 便捷属性，避免调用方重复写空检查
        public Vector3 CasterPosition => Caster != null ? Caster.transform.position : Vector3.zero;
        public TeamType CasterTeam => Caster != null ? Caster.Team : TeamType.Neutral;
    }
}
