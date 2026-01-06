using System;
using System.Collections.Generic;
using System.Linq;
using CDTU.Utils;
using UnityEngine;

namespace Character.Player.Skill.Evolution
{
    /// <summary>
    /// 进化效果池（全局单例，管理所有进化效果）
    /// 负责根据技能、等级、已选效果动态生成进化选项
    /// </summary>
    [CreateAssetMenu(fileName = "EvolutionEffectPool", menuName = "RogueGame/Skill/EvolutionPool")]
    public class EvolutionEffectPool : ScriptableObject
    {
        #region 配置

        [Header("所有进化效果")]
        [Tooltip("全局效果池，包含所有可能的进化效果")]
        public List<EvolutionEffectEntry> allEffects = new();

        [Header("生成选项配置")]
        [Tooltip("每次升级提供的选项数量（需与 UI 槽位数匹配）")]
        [Range(2, 4)]
        public int optionCount = 2;

        [Tooltip("确保至少有一个选项（如果可用效果不足，降低数量）")]
        public bool ensureAtLeastOne = true;

        [Header("动态权重（可选）")]
        [Tooltip("已选择的效果权重是否递减（鼓励多样化）")]
        public bool useDynamicWeight = true;

        [Tooltip("重复选择权重衰减系数（0.5 = 每重复一次权重减半）")]
        [Range(0f, 1f)]
        public float repeatDecayFactor = 0.5f;

        #endregion

        #region 私有字段

        /// <summary>
        /// 随机数生成器
        /// </summary>
        [NonSerialized]
        private System.Random _rng;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        [NonSerialized]
        private bool _isInitialized;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化效果池
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // 使用时间作为种子，保证每次运行不同
            _rng = new System.Random(System.DateTime.Now.Millisecond);

            // 验证配置
            ValidateConfig();

            _isInitialized = true;

            CDLogger.Log($"[EvolutionEffectPool] 初始化完成，效果数量: {allEffects.Count}");
        }

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        private void ValidateConfig()
        {
            int emptyCount = 0;
            int duplicateIdCount = 0;
            var idSet = new HashSet<string>();

            for (int i = 0; i < allEffects.Count; i++)
            {
                var effect = allEffects[i];

                // 检查空引用
                if (effect == null)
                {
                    CDLogger.LogWarning($"[EvolutionEffectPool] 索引 {i} 的效果为空");
                    emptyCount++;
                    continue;
                }

                // 检查 effectId
                if (string.IsNullOrEmpty(effect.effectId))
                {
                    CDLogger.LogWarning($"[EvolutionEffectPool] 索引 {i} 的效果缺少 effectId");
                }
                else if (idSet.Contains(effect.effectId))
                {
                    CDLogger.LogWarning($"[EvolutionEffectPool] 重复的 effectId: {effect.effectId}");
                    duplicateIdCount++;
                }
                else
                {
                    idSet.Add(effect.effectId);
                }
            }

            if (emptyCount > 0)
            {
                CDLogger.LogWarning($"[EvolutionEffectPool] 发现 {emptyCount} 个空效果");
            }

            if (duplicateIdCount > 0)
            {
                CDLogger.LogWarning($"[EvolutionEffectPool] 发现 {duplicateIdCount} 个重复的 effectId");
            }

            if (allEffects.Count == 0)
            {
                CDLogger.LogError("[EvolutionEffectPool] 效果池为空！请添加 EvolutionEffectEntry。");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 为指定技能生成升级选项
        /// </summary>
        /// <param name="skill">技能定义</param>
        /// <param name="currentLevel">当前等级（即将升级到的等级）</param>
        /// <param name="chosen">已选择的效果列表（使用 List 以正确支持重复计数）</param>
        /// <returns>可选效果列表</returns>
        public List<EvolutionEffectEntry> GetOptions(
            SkillDefinition skill,
            int currentLevel,
            System.Collections.Generic.IReadOnlyList<EvolutionEffectEntry> chosen)
        {
            // 延迟初始化
            if (!_isInitialized)
                Initialize();

            // 1. 筛选可用效果
            var available = allEffects
                .Where(e => e != null && e.weight > 0)
                .Where(e => e.IsAvailableFor(skill, currentLevel, chosen))
                .ToList();

            if (available.Count == 0)
            {
                CDLogger.LogWarning($"[EvolutionEffectPool] 没有可用的进化效果: {skill?.skillId} Lv{currentLevel}");
                return new List<EvolutionEffectEntry>();
            }

            // 2. 动态权重计算
            var weightedOptions = available.Select(e => new
            {
                effect = e,
                dynamicWeight = CalculateDynamicWeight(e, chosen)
            }).ToList();

            // 3. 权重随机抽取
            var options = new List<EvolutionEffectEntry>();
            var remaining = new List<(EvolutionEffectEntry effect, float weight)>(
                weightedOptions.Select(x => (x.effect, x.dynamicWeight))
            );

            // 确定实际选项数量（不超过可用数量）
            int actualOptionCount = Mathf.Min(optionCount, remaining.Count);

            // 如果可用效果少于请求数量且启用了 ensureAtLeastOne，记录日志
            if (ensureAtLeastOne && actualOptionCount < optionCount)
            {
                CDLogger.Log($"[EvolutionEffectPool] 可用效果不足，降低选项数: {optionCount} → {actualOptionCount}");
            }

            for (int i = 0; i < actualOptionCount; i++)
            {
                float totalWeight = remaining.Sum(x => x.weight);
                if (totalWeight <= 0.0001f) break;

                float random = (float)_rng.NextDouble() * totalWeight;
                float cumulative = 0f;
                int selectedIndex = -1;

                for (int j = 0; j < remaining.Count; j++)
                {
                    cumulative += remaining[j].weight;
                    if (random <= cumulative)
                    {
                        selectedIndex = j;
                        break;
                    }
                }

                if (selectedIndex >= 0)
                {
                    options.Add(remaining[selectedIndex].effect);
                    remaining.RemoveAt(selectedIndex);
                }
            }

            return options;
        }

        /// <summary>
        /// 根据 effectId 获取效果（用于保存加载）
        /// </summary>
        /// <param name="effectId">效果唯一标识</param>
        /// <returns>对应的效果条目，不存在则返回 null</returns>
        public EvolutionEffectEntry GetEffectById(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
                return null;

            return allEffects.FirstOrDefault(e => e != null && e.effectId == effectId);
        }

        /// <summary>
        /// 获取指定稀有度的所有效果
        /// </summary>
        public List<EvolutionEffectEntry> GetEffectsByRarity(EffectRarity rarity)
        {
            return allEffects
                .Where(e => e != null && e.rarity == rarity)
                .ToList();
        }

        /// <summary>
        /// 获取效果池统计信息
        /// </summary>
        public string GetPoolStatistics()
        {
            if (allEffects == null || allEffects.Count == 0)
                return "效果池为空";

            int commonCount = allEffects.Count(e => e != null && e.rarity == EffectRarity.Common);
            int rareCount = allEffects.Count(e => e != null && e.rarity == EffectRarity.Rare);
            int legendaryCount = allEffects.Count(e => e != null && e.rarity == EffectRarity.Legendary);

            return $"普通: {commonCount} | 稀有: {rareCount} | 传说: {legendaryCount} | 总计: {allEffects.Count}";
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算动态权重（考虑已选择次数）
        /// </summary>
        private float CalculateDynamicWeight(
            EvolutionEffectEntry effect,
            System.Collections.Generic.IReadOnlyList<EvolutionEffectEntry> chosen)
        {
            float weight = effect.weight;

            if (!useDynamicWeight || chosen == null || chosen.Count == 0)
                return weight;

            // 计算已选择次数（现在能正确计数，因为 List 支持重复）
            int chosenCount = chosen.Count(e => e != null && e.effectId == effect.effectId);

            // 动态衰减：每重复选择一次，权重乘以衰减系数
            for (int i = 0; i < chosenCount; i++)
            {
                weight *= repeatDecayFactor;
            }

            return weight;
        }

        #endregion
    }
}
