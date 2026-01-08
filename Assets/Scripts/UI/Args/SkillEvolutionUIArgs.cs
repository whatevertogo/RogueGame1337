using Game.UI;
using RogueGame.Events;

namespace UI
{
    /// <summary>
    /// 技能进化 UI 打开参数
    /// </summary>
    public class SkillEvolutionUIArgs : UIArgs<CardUpgradeView>
    {
        public readonly SkillEvolutionRequestedEvent Event;

        public SkillEvolutionUIArgs(SkillEvolutionRequestedEvent evt)
        {
            Event = evt;
        }
    }
}
