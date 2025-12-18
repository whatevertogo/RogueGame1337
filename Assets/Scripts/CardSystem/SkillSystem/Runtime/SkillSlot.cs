using UnityEngine;

namespace CardSystem.SkillSystem
{

    [System.Serializable]
    public class SkillSlot
    {
        // 可选：此槽位关联的 cardId（来自 GameRoot.Instance.CardDataBase），用于在运行时追踪槽位对应的卡牌实例/状态
        public string cardId;
        [SerializeField]
        public SkillDefinition skill;
        [ReadOnly] public float lastUseTime = -Mathf.Infinity;
        // 能量条（0-100）用于 Q/E 槽充能显示与释放判定
        [ReadOnly] public float energy = 0f;
        // 本房间是否已使用过（若为 true 则不能再次使用，直到进入新房间）
        [ReadOnly] public bool usedInCurrentRoom = false;
    }
}
