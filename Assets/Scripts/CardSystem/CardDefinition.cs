
using CardSystem.SkillSystem;
using UnityEngine;
using CardSystem.Card;

namespace CardSystem
{
    [CreateAssetMenu(fileName = "NewCardDefinition", menuName = "Card System/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        public string cardId;
        public CardType type; // Passive / Active
        public Sprite cardSprite;
        public ActiveCardConfig activeCardConfig;
        public PassiveCardConfig passiveCardConfig;


        [TextArea]
        public string Description;

        public string GetDescription() => Description;

        public Sprite GetSprite() => cardSprite;
    }
}
