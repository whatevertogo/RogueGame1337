namespace Character.Player.Skill.Modifiers
{
    /// <summary>
    /// 技能修改器优先级常量
    /// 数值越小越先执行
    /// </summary>
    public static class ModifierPriority
    {
        /// <summary>
        /// 能量消耗相关（最先计算）
        /// </summary>
        public const int ENERGY_COST = -100;

        /// <summary>
        /// 目标选择修改
        /// </summary>
        public const int TARGETING = -50;

        /// <summary>
        /// 伤害倍率
        /// </summary>
        public const int DAMAGE_MULTIPLIER = 0;

        /// <summary>
        /// 穿透/弹射等弹道修改
        /// </summary>
        public const int PROJECTILE = 10;

        /// <summary>
        /// 冷却修改
        /// </summary>
        public const int COOLDOWN = 50;

        /// <summary>
        /// 特效修改（最后应用）
        /// </summary>
        public const int VFX = 100;
    }
}
