
using CardSystem.SkillSystem;
using UnityEngine;

namespace CardSystem
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "Card System/Card Data")]
    public class CardData : ScriptableObject
    {
        public string cardId;
        public CardType type; // Passive / Active
        [InlineEditor]
        public SkillDefinition skill;
        public Sprite icon;

        [TextArea]
        public string Description;

        public string GetDescription() => Description;
    }
}