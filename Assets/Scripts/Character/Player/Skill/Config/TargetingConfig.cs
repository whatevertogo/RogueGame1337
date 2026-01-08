using System;

namespace Character.Player.Skill.Targeting
{
    /// <summary>
    /// 目标获取配置（独立结构体，用于 TargetingModifier）
    /// </summary>
    [Serializable]
    public struct TargetingConfig
    {
        /// <summary>
        /// 目标获取范围（由修改器修改）
        /// </summary>
        public float Range;

        /// <summary>
        /// 目标数量上限（由修改器修改）
        /// </summary>
        public int MaxCount;

        /// <summary>
        /// 目标获取半径（用于范围技能）
        /// </summary>
        public float Radius;

        /// <summary>
        /// 初始化默认值
        /// </summary>
        public static TargetingConfig Default => new TargetingConfig
        {
            Range = 10f,
            MaxCount = 1,
            Radius = 2f
        };
    }
}