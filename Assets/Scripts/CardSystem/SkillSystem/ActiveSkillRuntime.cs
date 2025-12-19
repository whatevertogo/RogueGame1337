
using System;

namespace CardSystem.SkillSystem
{

    [Serializable]
    public class ActiveSkillRuntime
    {
        public string CardId;
        public SkillDefinition Skill;

        // 链接到 InventoryManager 中的 ActiveCardState.instanceId
        public string InstanceId;
        public float LastUseTime;
        public bool UsedInCurrentRoom;

        public ActiveSkillRuntime(string cardId, SkillDefinition skill, string instanceId)
        {
            CardId = cardId;
            Skill = skill;
            InstanceId = instanceId;
            LastUseTime = -999f;
            UsedInCurrentRoom = false;
        }
    }
}