using System.Collections.Generic;
using UnityEngine;
    public abstract class TargetAcquireSO : ScriptableObject, ITargetAcquireStrategy
    {
        public abstract List<Transform> Acquire(SkillTargetContext ctx);
    }