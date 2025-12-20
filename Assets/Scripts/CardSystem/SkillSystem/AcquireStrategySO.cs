using System.Collections.Generic;
using Character;
using UnityEngine;


public abstract class TargetAcquireSO : ScriptableObject, ITargetAcquireStrategy
{
    public abstract List<CharacterBase> Acquire(SkillTargetContext ctx);
}