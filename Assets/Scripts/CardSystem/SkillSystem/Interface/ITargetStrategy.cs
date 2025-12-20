
using System.Collections.Generic;
using UnityEngine;

    public interface ITargetAcquireStrategy
    {
        List<Transform> Acquire(SkillTargetContext ctx);
    }