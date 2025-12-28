
using System.Collections.Generic;
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;

    public interface ITargetAcquireStrategy
    {
        List<CharacterBase> Acquire(SkillTargetContext ctx);
    }