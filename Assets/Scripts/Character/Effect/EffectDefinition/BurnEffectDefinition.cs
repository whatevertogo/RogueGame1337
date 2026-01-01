using Character.Effects;
using UnityEngine;
using Character;


    /// <summary>
    /// 灼烧效果定义（持续伤害效果）
    /// </summary>
    [CreateAssetMenu(fileName = "Burn", menuName = "RogueGame/Character/Effects/Debuffs/Burn")]
    public class BurnEffectDefinitionSO : StatusEffectDefinitionSO
    {
        [Header("灼烧设置")]
        [Tooltip("每秒伤害")]
        public float damagePerSecond = 5f;

        public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
        {
            return new BurnEffectInstance(this, caster);
        }

}