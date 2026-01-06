using System.Collections.Generic;
using System.Linq;
using Character.Player.Skill.Modifiers;
using UnityEngine;

namespace Character.Player.Skill.Evolution
{
    /// <summary>
    /// 进化效果条目（效果池中的单项）
    /// 每个效果定义了一个可选的技能进化选项
    /// </summary>
    [CreateAssetMenu(fileName = "EvolutionEffect", menuName = "RogueGame/Skill/EvolutionEffect")]
    public class EvolutionEffectEntry : ScriptableObject
    {
        #region 基本信息

        [Header("唯一标识")]
        [Tooltip("效果唯一标识（用于保存，不可修改）")]
        public string effectId;

        [Header("基本信息")]
        [Tooltip("效果名称（UI 显示）")]
        public string effectName;

        [TextArea(3, 6)]
        [Tooltip("效果描述（UI 显示）")]
        public string description;

        [Tooltip("效果图标（UI 显示）")]
        public Sprite icon;

        #endregion

        #region 稀有度配置

        [Header("稀有度")]
        [Tooltip("效果稀有度，决定最低等级要求和 UI 颜色")]
        public EffectRarity rarity = EffectRarity.Common;

        #endregion

        #region 效果配置

        [Header("效果修改器")]
        [Tooltip("实际的修改器（复用现有修改器系统）")]
        public SkillModifierBase modifier;

        #endregion

        #region 标签匹配

        [Header("标签兼容性")]
        [Tooltip("只有包含这些标签的技能才能随机到此效果（空=不限制）")]
        public List<SkillTag> requiredTags = new();

        [Tooltip("包含这些标签的技能不会随机到此效果")]
        public List<SkillTag> excludedTags = new();

        #endregion

        #region 概率控制

        [Header("出现权重")]
        [Tooltip("基础权重，越高越容易出现（0=不出现）")]
        public float weight = 1f;

        #endregion

        #region 条件限制

        [Header("等级限制")]
        [Tooltip("最低等级要求（根据稀有度自动设置，可手动调整）")]
        public int minLevel = 2;

        [Tooltip("最高等级限制（0=无限制）")]
        public int maxLevel = 0;

        [Header("叠加限制")]
        [Tooltip("同一技能最多能选择此效果多少次（0=不限制）")]
        public int maxStacks = 1;

        #endregion

        #region 编辑器辅助

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中根据稀有度自动设置 minLevel
        /// </summary>
        void OnValidate()
        {
            int requiredMin = rarity.MinLevel();
            if (minLevel < requiredMin)
            {
                minLevel = requiredMin;
            }
        }
#endif

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查是否可应用于指定技能
        /// </summary>
        /// <param name="skill">技能定义</param>
        /// <param name="currentLevel">当前等级</param>
        /// <param name="chosen">已选择的效果列表（使用 List 以正确支持重复计数）</param>
        /// <returns>是否可用</returns>
        public bool IsAvailableFor(
            SkillDefinition skill,
            int currentLevel,
            IReadOnlyList<EvolutionEffectEntry> chosen)
        {
            // 1. 等级检查
            if (currentLevel < minLevel)
                return false;

            if (maxLevel > 0 && currentLevel > maxLevel)
                return false;

            // 2. 标签兼容检查
            if (requiredTags != null && requiredTags.Count > 0)
            {
                if (!skill.HasAnyTag(requiredTags))
                    return false;
            }

            // 3. 标签排斥检查
            if (excludedTags != null && excludedTags.Count > 0)
            {
                if (skill.HasAnyTag(excludedTags))
                    return false;
            }

            // 4. 叠加检查
            if (maxStacks > 0)
            {
                int currentCount = chosen.Count(e => e != null && e.effectId == this.effectId);
                if (currentCount >= maxStacks)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取稀有度颜色（UI 显示用）
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity.GetColor();
        }

        /// <summary>
        /// 获取稀有度颜色代码（HTML 格式）
        /// </summary>
        public string GetRarityColorHex()
        {
            return rarity.GetColorHex();
        }

        /// <summary>
        /// 获取稀有度显示名称
        /// </summary>
        public string GetRarityDisplayName()
        {
            return rarity.GetDisplayName();
        }

        #endregion
    }
}
