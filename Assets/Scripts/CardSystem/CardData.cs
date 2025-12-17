
using CardSystem.SkillSystem;
using UnityEngine;

namespace CardSystem
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "Card System/Card Data")]
    public class CardData : ScriptableObject
    {
        public string cardId;
        public CardType type; // Passive / Active

        // ========== Active card runtime charge config (optional) ==========
        // 这些字段只在主动卡有效（编辑器可配置），运行时的当前充能会保存在 RunInventory 的运行时 state（ActiveCardState）中。
        [Header("Active Card (optional) - charge config")]
        [Tooltip("初始充能（运行时 state 会维护当前充能）")]
        public int initialCharges = 0;
        [Tooltip("最大充能")]
        public int maxCharges = 3;
        [Tooltip("击杀一名敌人时获得的充能（每次击杀）")]
        public int chargesPerKill = 1;
        [Tooltip("若为 true 则此卡需要消耗充能才能使用；否则可按冷却使用")]
        public bool requiresCharge = true;

        [InlineEditor]
        public SkillDefinition skill;
        public Sprite icon;

        [TextArea]
        public string Description;

        public string GetDescription() => Description;
    }
}
