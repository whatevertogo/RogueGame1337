using Character;
using UnityEngine;

namespace RogueGame.GameConfig
{
    /// <summary>
    /// 属性上限配置
    /// 定义游戏中各种属性的最大值限制，防止数值爆炸
    /// </summary>
    [CreateAssetMenu(fileName = "StatLimit", menuName = "RogueGame/Game/Stat Limit Config")]
    public class StatLimitConfig : ScriptableObject
    {
        #region 生命属性上限

        [Header("生命属性上限")]
        [Tooltip("最大生命值上限")]
        [Min(1)]
        public int maxMaxHP = 10000;

        [Tooltip("生命恢复上限（每秒）")]
        [Min(0)]
        public float maxHPRegen = 100f;

        #endregion

        #region 移动属性上限

        [Header("移动属性上限")]
        [Tooltip("移动速度上限")]
        [Min(1)]
        public float maxMoveSpeed = 20f;

        [Tooltip("加速度上限")]
        [Min(1)]
        public float maxAcceleration = 50f;

        #endregion

        #region 攻击属性上限

        [Header("攻击属性上限")]
        [Tooltip("攻击力上限")]
        [Min(1)]
        public float maxAttackPower = 1000f;

        [Tooltip("攻击速度上限")]
        [Min(0.1f)]
        public float maxAttackSpeed = 5f;

        [Tooltip("攻击范围上限")]
        [Min(1)]
        public float maxAttackRange = 10f;

        #endregion

        #region 防御属性上限

        [Header("防御属性上限")]
        [Tooltip("护甲值上限")]
        [Min(0)]
        public float maxArmor = 500f;

        [Tooltip("闪避率上限（0-1之间，例如0.8表示80%）")]
        [Range(0f, 1f)]
        public float maxDodge = 0.75f; // 建议75%，避免无敌

        #endregion

        #region 技能属性上限

        [Header("技能属性上限")]
        [Tooltip("技能冷却缩减上限（0-1之间，例如0.5表示50%）")]
        [Range(0f, 1f)]
        public float maxSkillCooldownReduction = 0.5f;

        #endregion

        #region 卡牌上限

        [Header("卡牌数量上限")]
        [Tooltip("被动卡牌叠加上限（同一张被动卡最多叠加数量）")]
        [Min(1)]
        public int maxPassiveCardStack = 99;

        [Tooltip("主动技能等级上限")]
        [Min(1)]
        public int maxActiveSkillLevel = 5;

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取指定属性类型的上限值
        /// </summary>
        /// <param name="statType">属性类型</param>
        /// <returns>上限值，如果没有限制返回null</returns>
        public float? GetLimit(StatType statType)
        {
            return statType switch
            {
                StatType.MaxHP => maxMaxHP,
                StatType.HPRegen => maxHPRegen,
                StatType.MoveSpeed => maxMoveSpeed,
                StatType.Acceleration => maxAcceleration,
                StatType.AttackPower => maxAttackPower,
                StatType.AttackSpeed => maxAttackSpeed,
                StatType.AttackRange => maxAttackRange,
                StatType.Armor => maxArmor,
                StatType.Dodge => maxDodge,
                StatType.SkillCooldownReductionRate => maxSkillCooldownReduction,
                _ => null
            };
        }

        /// <summary>
        /// 获取限制后的值（如果超过上限则返回上限值）
        /// </summary>
        /// <param name="statType">属性类型</param>
        /// <param name="value">原始值</param>
        /// <returns>限制后的值</returns>
        public float ClampValue(StatType statType, float value)
        {
            float? limit = GetLimit(statType);
            if (limit.HasValue)
            {
                return Mathf.Clamp(value, -limit.Value, limit.Value);
            }
            return value;
        }

        #endregion
    }
}