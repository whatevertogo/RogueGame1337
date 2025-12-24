using UnityEngine;

namespace RogueGame.GameConfig
{
    /// <summary>
    /// 游戏难度曲线配置
    /// 定义随层数递增的难度缩放参数，支持线性和指数两种增长模式
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyCurveConfig", menuName = "Game/Difficulty Curve")]
    public class DifficultyCurveConfig : ScriptableObject
    {
        #region 基础属性缩放

        [Header("基础属性缩放（每层递增百分比）")]
        [Tooltip("每层生命值递增百分比（例如 0.1 = +10%）")]
        [Range(0f, 0.5f)]
        public float hpGrowthPerFloor = 0.1f;

        [Tooltip("每层攻击力递增百分比（例如 0.05 = +5%）")]
        [Range(0f, 0.3f)]
        public float attackPowerGrowthPerFloor = 0.05f;

        [Tooltip("每层防御力递增百分比（例如 0.03 = +3%）")]
        [Range(0f, 0.2f)]
        public float defenseGrowthPerFloor = 0.03f;

        [Tooltip("每层攻击速度递增百分比（例如 0.02 = +2%）")]
        [Range(0f, 0.15f)]
        public float attackSpeedGrowthPerFloor = 0.02f;

        #endregion

        #region 敌人类型额外倍率

        [Header("敌人类型额外倍率")]
        [Tooltip("精英敌人属性倍率（在基础缩放之上）")]
        [Range(1f, 5f)]
        public float eliteMultiplier = 2f;

        [Tooltip("Boss 属性倍率（在基础缩放之上）")]
        [Range(1f, 10f)]
        public float bossMultiplier = 5f;

        #endregion

        #region 难度增长设置

        [Header("难度增长设置")]
        [Tooltip("难度缩放生效的起始层数（前几层不增加难度）")]
        [Min(1)]
        public int startFloor = 2;

        [Tooltip("难度增长类型")]
        public DifficultyGrowthType growthType = DifficultyGrowthType.Linear;

        [Tooltip("指数曲线的底数（仅当 Exponential 模式时生效）")]
        [Range(1.05f, 1.5f)]
        public float exponentialBase = 1.1f;

        [Tooltip("最大难度缩放上限（防止后期数值爆炸）")]
        [Min(1f)]
        public float maxDifficultyScaling = 5f;

        #endregion

        #region 敌人数量缩放

        [Header("敌人数量缩放")]
        [Tooltip("每层敌人数量递增百分比（例如 0.1 = +10%）")]
        [Range(0f, 0.5f)]
        public float enemyCountGrowthPerFloor = 0.1f;

        [Tooltip("敌人数量上限（避免过多敌人影响性能）")]
        [Min(1)]
        public int maxEnemyCount = 20;

        #endregion

        #region 公式方法

        /// <summary>
        /// 计算指定层数的基础难度缩放系数
        /// </summary>
        /// <param name="floor">当前层数</param>
        /// <returns>难度缩放系数（1.0 表示无缩放）</returns>
        public float GetDifficultyScaling(int floor)
        {
            // 在起始层之前不增加难度
            if (floor < startFloor)
                return 1f;

            int effectiveFloors = floor - startFloor + 1;

            float scaling;
            if (growthType == DifficultyGrowthType.Exponential)
            {
                // 指数增长：base ^ (floor - startFloor)
                scaling = Mathf.Pow(exponentialBase, effectiveFloors - 1);
            }
            else
            {
                // 线性增长：1 + (floor - startFloor) * 0.1（每层 +10%）
                scaling = 1f + (effectiveFloors - 1) * 0.1f;
            }

            // 应用最大上限
            return Mathf.Min(scaling, maxDifficultyScaling);
        }

        /// <summary>
        /// 获取生命值缩放系数
        /// </summary>
        /// <param name="floor">当前层数</param>
        /// <returns>生命值缩放系数</returns>
        public float GetHpMultiplier(int floor)
        {
            if (floor < startFloor)
                return 1f;

            float baseScaling = GetDifficultyScaling(floor);
            float hpScaling = 1f + (floor - startFloor + 1) * hpGrowthPerFloor;
            return baseScaling * hpScaling;
        }

        /// <summary>
        /// 获取攻击力缩放系数
        /// </summary>
        /// <param name="floor">当前层数</param>
        /// <returns>攻击力缩放系数</returns>
        public float GetAttackPowerMultiplier(int floor)
        {
            if (floor < startFloor)
                return 1f;

            float baseScaling = GetDifficultyScaling(floor);
            float atkScaling = 1f + (floor - startFloor + 1) * attackPowerGrowthPerFloor;
            return baseScaling * atkScaling;
        }

        /// <summary>
        /// 获取防御力缩放系数
        /// </summary>
        /// <param name="floor">当前层数</param>
        /// <returns>防御力缩放系数</returns>
        public float GetDefenseMultiplier(int floor)
        {
            if (floor < startFloor)
                return 1f;

            float baseScaling = GetDifficultyScaling(floor);
            float defScaling = 1f + (floor - startFloor + 1) * defenseGrowthPerFloor;
            return baseScaling * defScaling;
        }

        /// <summary>
        /// 获取攻击速度缩放系数
        /// </summary>
        /// <param name="floor">当前层数</param>
        /// <returns>攻击速度缩放系数</returns>
        public float GetAttackSpeedMultiplier(int floor)
        {
            if (floor < startFloor)
                return 1f;

            float baseScaling = GetDifficultyScaling(floor);
            float speedScaling = 1f + (floor - startFloor + 1) * attackSpeedGrowthPerFloor;
            return baseScaling * speedScaling;
        }

        /// <summary>
        /// 获取指定敌人类型的完整属性缩放系数（包含类型倍率）
        /// </summary>
        /// <param name="floor">当前层数</param>
        /// <param name="enemyType">敌人类型</param>
        /// <returns>最终属性缩放系数</returns>
        public float GetAttributeScaling(int floor, EnemyType enemyType)
        {
            float baseScaling = GetDifficultyScaling(floor);

            // 应用敌人类型倍率
            float typeMultiplier = enemyType switch
            {
                EnemyType.Elite => eliteMultiplier,
                EnemyType.Boss => bossMultiplier,
                _ => 1f
            };

            return baseScaling * typeMultiplier;
        }

        /// <summary>
        /// 计算缩放后的敌人数量
        /// </summary>
        /// <param name="floor">当前层数</param>
        /// <param name="baseCount">基础敌人数量</param>
        /// <returns>缩放后的敌人数量</returns>
        public int GetScaledEnemyCount(int floor, int baseCount)
        {
            if (floor < startFloor)
                return baseCount;

            float scaling = 1f + (floor - startFloor + 1) * enemyCountGrowthPerFloor;
            int scaledCount = Mathf.RoundToInt(baseCount * scaling);
            return Mathf.Min(scaledCount, maxEnemyCount);
        }

        #endregion
    }

    /// <summary>
    /// 难度增长类型
    /// </summary>
    public enum DifficultyGrowthType
    {
        /// <summary>线性增长（每层固定增量）</summary>
        Linear,

        /// <summary>指数增长（每层固定倍率）</summary>
        Exponential
    }

    /// <summary>
    /// 敌人类型枚举
    /// </summary>
    public enum EnemyType
    {
        /// <summary>普通敌人</summary>
        Normal,

        /// <summary>精英敌人</summary>
        Elite,

        /// <summary>Boss</summary>
        Boss
    }
}
