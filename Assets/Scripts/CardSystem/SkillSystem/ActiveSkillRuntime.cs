
using System;

namespace CardSystem.SkillSystem
{

    [Serializable]
    public class ActiveSkillRuntime
    {
        public string CardId;
        public SkillDefinition Skill;

        public float Energy;
        public float LastUseTime;
        public bool UsedInCurrentRoom;

        public ActiveSkillRuntime(string cardId, SkillDefinition skill)
        {
            CardId = cardId;
            Skill = skill;
            Energy = 0f;
            LastUseTime = -999f;
            UsedInCurrentRoom = false;
        }
    }
}