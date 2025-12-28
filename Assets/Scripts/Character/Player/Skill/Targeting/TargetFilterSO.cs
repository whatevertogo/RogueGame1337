using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;

public abstract class TargetFilterSO : ScriptableObject
{
    public abstract bool IsValid(SkillTargetContext ctx, CharacterBase target);
}