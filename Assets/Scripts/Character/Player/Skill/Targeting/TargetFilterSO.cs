
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;

[ManagedData("Skill")]
public abstract class TargetFilterSO : ScriptableObject
{
    public abstract bool IsValid (SkillContext ctx, CharacterBase target);
}