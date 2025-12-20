using System.Collections.Generic;
using Character;
using UnityEngine;

/// <summary>
/// 运行时传递给 SkillExecutor 的上下文
/// </summary>
public struct SkillContext
{
    public CharacterBase Caster;
    public IReadOnlyList<CharacterBase> Targets;
    public Vector3? AimPoint;
    public int SlotIndex;
    public string CardId;
}