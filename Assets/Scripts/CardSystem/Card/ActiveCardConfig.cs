
using System;
using CardSystem;
using CardSystem.SkillSystem;
using UnityEngine;

namespace CardSystem.Card
{
    [Serializable]
    public class ActiveCardConfig
    {
        [Tooltip("最大充能")]
        public int maxCharges = 3;
        [Tooltip("击杀一名敌人时获得的充能（每次击杀）")]
        public int chargesPerKill = 1;
        [Tooltip("若为 true 则此卡需要消耗充能才能使用；否则可按冷却使用")]
        public bool requiresCharge = true;

        [InlineEditor]
        public SkillDefinition skill;
        public Sprite icon;
    }
}