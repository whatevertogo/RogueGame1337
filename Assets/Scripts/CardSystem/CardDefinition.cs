
using CardSystem.SkillSystem;
using UnityEngine;

namespace CardSystem
{
    [CreateAssetMenu(fileName = "NewCardDefinition", menuName = "Card System/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        public string cardId;
        public CardType type; // Passive / Active

        [TextArea]
        public string Description;

        public string GetDescription() => Description;
    }
}
