
using UnityEngine;


[ManagedData("Card")]
[CreateAssetMenu(fileName = "CardDefinition", menuName = "RogueGame/Card/Definition")]
public class CardDefinition : ScriptableObject
{
    public string CardId;
    public CardType CardType; // Passive / Active
    public Sprite cardSprite;

    public ActiveCardConfig activeCardConfig;

    public PassiveCardConfig passiveCardConfig;
    public int Cost;

    [TextArea]
    public string Description;

    public string GetDescription() => Description;

    public Sprite GetSprite() => cardSprite;
}
