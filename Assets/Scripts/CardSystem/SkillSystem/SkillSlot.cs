using UnityEngine;

namespace CardSystem.SkillSystem
{

    [System.Serializable]
    public class SkillSlot
    {
        // 可选：此槽位关联的 cardId（来自 CardRegistry），用于在运行时追踪槽位对应的卡牌实例/状态
        public string cardId;
        public SkillDefinition skill;
        [ReadOnly] public float lastUseTime = -Mathf.Infinity;
    }
}
