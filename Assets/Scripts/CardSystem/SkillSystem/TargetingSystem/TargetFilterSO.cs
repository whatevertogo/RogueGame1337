using Character;
using UnityEngine;

public abstract class TargetFilterSO : ScriptableObject
{
    public abstract bool IsValid(SkillTargetContext ctx, CharacterBase target);
}