using System.Collections.Generic;
using Character;
using UnityEngine;

public struct SkillTargetContext
{
    public CharacterBase Caster;
    public Vector3 AimPoint;

    // 便捷属性，避免调用方重复写空检查
    public Vector3 CasterPosition => Caster != null ? Caster.transform.position : Vector3.zero;
    public TeamType CasterTeam => Caster != null ? Caster.Team : TeamType.Neutral;
}
