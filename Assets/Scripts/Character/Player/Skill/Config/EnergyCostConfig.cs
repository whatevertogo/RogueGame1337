using System;
using UnityEngine;
namespace Character.Player.Skill.Targeting
{
    /// <summary>
    /// 能量消耗配置（独立结构体，用于 CostModifier）
    /// </summary>
    [Serializable]
    public struct EnergyCostConfig
    {
        /// <summary>
        /// 能量消耗倍率（由修改器修改，默认 1.0）
        /// </summary>
        public float Multiplier;

        /// <summary>
        /// 能量消耗固定加值（由修改器修改）
        /// </summary>
        public int Flat;

        /// <summary>
        /// 计算最终能量消耗（仅用于 Threshold 模式）
        /// </summary>
        public int CalculateFinalCost(int baseCost)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseCost * Multiplier) + Flat);
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        public static EnergyCostConfig Default => new EnergyCostConfig
        {
            Multiplier = 1.0f,
            Flat = 0
        };
    }
}