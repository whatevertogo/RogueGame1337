
using System.Collections.Generic;
using Character;
using UnityEngine;

    public interface ITargetAcquireStrategy
    {
        List<CharacterBase> Acquire(SkillTargetContext ctx);
    }