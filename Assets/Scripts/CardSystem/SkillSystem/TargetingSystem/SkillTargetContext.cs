using System.Collections.Generic;
using Character;
using UnityEngine;

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

    // 便捷属性，避免调用方重复写空检查
    public Vector3 CasterPosition => Caster != null ? Caster.transform.position : Vector3.zero;
    public TeamType CasterTeam => Caster != null ? Caster.Team : TeamType.Neutral;
}
