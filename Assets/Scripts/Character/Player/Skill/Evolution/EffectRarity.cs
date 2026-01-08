using UnityEngine;

namespace Character.Player.Skill.Evolution
{
    /// <summary>
    /// 进化效果稀有度
    /// 决定效果的最低解锁等级和 UI 显示颜色
    /// </summary>
    public enum EffectRarity
    {
        /// <summary>
        /// 普通（Lv2+ 可出现）
        /// 基础数值效果，如伤害+10%、范围+20% 等
        /// </summary>
        Common,

        /// <summary>
        /// 稀有（Lv5+ 可出现）
        /// 复合效果或高数值效果，如伤害+范围双加、状态附着等
        /// </summary>
        Rare,

        /// <summary>
        /// 传说（Lv10+ 可出现）
        /// 规则质变效果，如无限释放、双重释放、技能联动等
        /// </summary>
        Legendary
    }

    /// <summary>
    /// EffectRarity 扩展方法
    /// </summary>
    public static class EffectRarityExtensions
    {
        /// <summary>
        /// 获取稀有度对应的最低等级要求
        /// </summary>
        public static int MinLevel(this EffectRarity rarity)
        {
            return rarity switch
            {
                EffectRarity.Common => 2,
                EffectRarity.Rare => 5,
                EffectRarity.Legendary => 10,
                _ => 2
            };
        }

        /// <summary>
        /// 获取稀有度对应的显示颜色
        /// </summary>
        public static Color GetColor(this EffectRarity rarity)
        {
            return rarity switch
            {
                EffectRarity.Common => new Color(0.9f, 0.9f, 0.9f),    // 白色
                EffectRarity.Rare => new Color(0.3f, 0.6f, 1.0f),      // 蓝色
                EffectRarity.Legendary => new Color(1.0f, 0.8f, 0.2f), // 金色
                _ => Color.white
            };
        }

        /// <summary>
        /// 获取稀有度对应的颜色代码（HTML 格式，用于 UI 文本）
        /// </summary>
        public static string GetColorHex(this EffectRarity rarity)
        {
            return rarity switch
            {
                EffectRarity.Common => "#E6E6E6",
                EffectRarity.Rare => "#4D99FF",
                EffectRarity.Legendary => "#FFCC33",
                _ => "#FFFFFF"
            };
        }

        /// <summary>
        /// 获取稀有度对应的显示名称（中文）
        /// </summary>
        public static string GetDisplayName(this EffectRarity rarity)
        {
            return rarity switch
            {
                EffectRarity.Common => "普通",
                EffectRarity.Rare => "稀有",
                EffectRarity.Legendary => "传说",
                _ => "未知"
            };
        }
    }
}
