using UnityEngine;

namespace CardSystem.SkillSystem
{

    [System.Serializable]
    public class SkillSlot
    {
        public SkillDefinition skill;
        [ReadOnly] public float lastUseTime = -Mathf.Infinity;
    }
}