using System;
using System.Collections.Generic;
using System.Linq;
using Character.Player.Skill.Evolution;
using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Targeting;
using UnityEngine;

namespace Character.Player.Skill.Runtime
{
    /// <summary>
    /// 主动技能运行时状态：支持修改器和进化树（纯充能系统，无CD）
    /// </summary>
    [Serializable]
    public class ActiveSkillRuntime
    {
        // ========== 基础字段 ==========
        public string CardId;
        public SkillDefinition Skill;

        // 链接到 InventoryServiceManager 中的 ActiveCardState.instanceId
        public string InstanceId;

        // 运行时协程引用（非序列化）用于取消延迟/选择流程
        [NonSerialized]
        public Coroutine RunningCoroutine;

        /// <summary>
        /// 实际消耗的能量值（用于协程取消时退还，包含修改器影响）
        /// </summary>
        [NonSerialized]
        public int ActualEnergyConsumed;


        /// <summary>
        /// 能量已消耗标志（用于技能取消时退还能量）
        /// </summary>
        [NonSerialized]
        public bool EnergyConsumed;

        // ========== 修改器系统 ==========
        [NonSerialized]
        private List<ISkillModifier> _activeModifiers = new List<ISkillModifier>();

        /// <summary>
        /// 获取所有活动修改器（只读，防止外部直接修改）
        /// </summary>
        public IReadOnlyList<ISkillModifier> ActiveModifiers => _activeModifiers;

        // ========== 进化状态（效果池系统） ==========
        /// <summary>
        /// 已选择的进化效果列表（运行时，不序列化）
        /// 保存时使用 ActiveCardState.ChosenEffectIds（只保存 effectId）
        /// </summary>
        [NonSerialized]
        private List<EvolutionEffectEntry> _chosenEvolutions = new List<EvolutionEffectEntry>();

        /// <summary>
        /// 获取已选择的进化效果列表（只读，防止外部直接修改）
        /// </summary>
        public IReadOnlyList<EvolutionEffectEntry> ChosenEvolutions => _chosenEvolutions;

        // ========== 卡牌配置缓存 ==========
        /// <summary>
        /// 缓存的卡牌定义（避免频繁从 CardDatabase.Resolve 查找）
        /// </summary>
        [NonSerialized]
        private CardDefinition _cachedCardDefinition;

        /// <summary>
        /// 获取缓存的卡牌定义（懒加载）
        /// </summary>
        public CardDefinition CachedCardDefinition
        {
            get
            {
                if (_cachedCardDefinition == null && !string.IsNullOrEmpty(CardId))
                {
                    _cachedCardDefinition = GameRoot.Instance?.CardDatabase?.Resolve(CardId);
                }
                return _cachedCardDefinition;
            }
        }

        /// <summary>
        /// 获取缓存的 ActiveCardConfig（便捷访问）
        /// </summary>
        public ActiveCardConfig CachedActiveConfig => CachedCardDefinition?.activeCardConfig;

        // ========== 构造函数 ==========
        public ActiveSkillRuntime(string cardId, SkillDefinition skill, string instanceId)
        {
            CardId = cardId;
            Skill = skill;
            InstanceId = instanceId;
        }

        #region 修改器管理
        /// <summary>
        /// 添加修改器（惰性排序，在应用时才排序）
        /// </summary>
        public void AddModifier(ISkillModifier modifier)
        {
            if (modifier == null) return;
            if (_activeModifiers.Contains(modifier)) return;

            _activeModifiers.Add(modifier);

            // 如果是效果生成修改器，标记效果缓存为脏
            if (modifier is IEffectGeneratorModifier)
            {
                MarkEffectsCacheDirty();
            }
        }

        /// <summary>
        /// 移除指定修改器
        /// </summary>
        public bool RemoveModifier(ISkillModifier modifier)
        {
            bool removed = _activeModifiers.Remove(modifier);

            // 如果移除的是效果生成修改器，标记效果缓存为脏
            if (removed && modifier is IEffectGeneratorModifier)
            {
                MarkEffectsCacheDirty();
            }

            return removed;
        }

        /// <summary>
        /// 清除所有修改器
        /// </summary>
        public void ClearAllModifiers()
        {
            _activeModifiers.Clear();
            MarkEffectsCacheDirty();
        }

        #endregion

        #region 阶段化修改器应用

        /// <summary>
        /// 阶段1：应用能量消耗修改器
        /// 在能量消耗前调用，按优先级递减排序
        /// </summary>
        public void ApplyEnergyCostModifiers(ref EnergyCostConfig config)
        {
            // 按优先级递减排序（高优先级先执行）
            var sortedModifiers = _activeModifiers
                .OfType<IEnergyCostModifier>()
                .OrderByDescending(m => m.Priority);

            foreach (var modifier in sortedModifiers)
            {
                modifier.ApplyEnergyCost(this, ref config);
            }
        }

        /// <summary>
        /// 阶段2：应用目标获取修改器
        /// 在目标获取前调用，按优先级递减排序
        /// </summary>
        public void ApplyTargetingModifiers(ref TargetingConfig config)
        {
            var sortedModifiers = _activeModifiers
                .OfType<ITargetingModifier>()
                .OrderByDescending(m => m.Priority);

            foreach (var modifier in sortedModifiers)
            {
                modifier.ApplyTargeting(this, ref config);
            }
        }

        /// <summary>
        /// 阶段4：应用伤害修改器
        /// 在应用效果前调用，按优先级递减排序
        /// </summary>
        public void ApplyDamageModifiers( DamageResult result)
        {
            var sortedModifiers = _activeModifiers
                .OfType<IDamageModifier>()
                .OrderByDescending(m => m.Priority);

            foreach (var modifier in sortedModifiers)
            {
                modifier.ApplyDamage(this, ref result);
            }
        }

        /// <summary>
        /// 阶段6：应用跨阶段修改器
        /// 在所有阶段完成后调用（用于处理跨阶段逻辑），按优先级递减排序
        /// </summary>
        public void ApplyCrossPhaseModifiers( SkillContext ctx)
        {
            var sortedModifiers = _activeModifiers
                .OfType<ICrossPhaseModifier>()
                .OrderByDescending(m => m.Priority);

            foreach (var modifier in sortedModifiers)
            {
                modifier.ApplyCrossPhase(this, ctx);
            }
        }

        #endregion

        #region 效果生成（新增）

        // ========== 效果缓存 ==========
        /// <summary>
        /// 缓存的效果列表（避免重复计算）
        /// </summary>
        [NonSerialized]
        private List<StatusEffectDefinitionSO> _cachedEffects;

        /// <summary>
        /// 效果缓存失效标志
        /// </summary>
        [NonSerialized]
        private bool _effectsCacheDirty = true;

        /// <summary>
        /// 获取所有效果（基础 + 修改器生成）
        /// 使用缓存机制，避免重复计算
        /// </summary>
        public List<StatusEffectDefinitionSO> GetAllEffects()
        {
            if (_effectsCacheDirty || _cachedEffects == null)
            {
                _cachedEffects = CalculateAllEffects();
                _effectsCacheDirty = false;
            }
            return _cachedEffects;
        }

        /// <summary>
        /// 计算所有效果（基础效果 + 修改器生成的效果）
        /// </summary>
        private List<StatusEffectDefinitionSO> CalculateAllEffects()
        {
            var allEffects = new List<StatusEffectDefinitionSO>();

            // 1. 添加技能定义的基础效果
            if (Skill?.Effects != null)
            {
                allEffects.AddRange(Skill.Effects);
            }

            // 2. 从所有效果生成修改器中收集效果
            var effectGenerators = _activeModifiers.OfType<IEffectGeneratorModifier>();
            foreach (var generator in effectGenerators)
            {
                var generatedEffects = generator.GenerateEffects(this);
                if (generatedEffects != null && generatedEffects.Count > 0)
                {
                    allEffects.AddRange(generatedEffects);
                }
            }

            return allEffects;
        }

        /// <summary>
        /// 标记效果缓存为脏（在修改器变化时调用）
        /// </summary>
        public void MarkEffectsCacheDirty()
        {
            _effectsCacheDirty = true;
        }

        #endregion

        // ========== 进化效果池系统 ==========

        /// <summary>
        /// 应用进化效果（效果池系统）
        /// 从玩家选择的 EvolutionEffectEntry 中提取修改器并应用
        /// </summary>
        /// <param name="entry">要应用的进化效果</param>
        public void ApplyEvolution(EvolutionEffectEntry entry)
        {
            if (entry == null)
            {
                Debug.LogWarning("[ActiveSkillRuntime] 尝试应用空的进化效果");
                return;
            }

            // 1. 添加到已选择列表（List 允许重复，支持 maxStacks > 1）
            _chosenEvolutions.Add(entry);

            // 2. 如果效果包含修改器，应用到技能
            // 注意：同一修改器只添加一次（避免重复），但效果可以多次选择
            // 叠加效果可以通过修改器内部的堆叠逻辑实现
            if (entry.modifier is ISkillModifier modifier)
            {
                AddModifier(modifier);
                int stackCount = _chosenEvolutions.Count(e => e?.effectId == entry.effectId);
                Debug.Log($"[ActiveSkillRuntime] 应用进化效果: {entry.effectName} (Lv{CurrentLevel}, 叠加层数: {stackCount})");
            }
            else
            {
                Debug.LogWarning($"[ActiveSkillRuntime] 进化效果 {entry.effectName} 没有有效的修改器");
            }
        }

        /// <summary>
        /// 从存档恢复进化效果
        /// 在游戏加载时调用，从保存的 effectId 列表恢复效果
        /// </summary>
        /// <param name="effectIds">保存的效果 ID 列表</param>
        /// <param name="pool">全局效果池，用于通过 ID 查找效果</param>
        public void LoadEvolutionFromSave(IReadOnlyList<string> effectIds, EvolutionEffectPool pool)
        {
            if (effectIds == null || pool == null)
            {
                Debug.LogWarning("[ActiveSkillRuntime] LoadEvolutionFromSave: effectIds 或 pool 为空");
                return;
            }

            _chosenEvolutions.Clear();

            foreach (string effectId in effectIds)
            {
                if (string.IsNullOrEmpty(effectId))
                    continue;

                EvolutionEffectEntry effect = pool.GetEffectById(effectId);
                if (effect != null)
                {
                    _chosenEvolutions.Add(effect);

                    // 恢复修改器
                    if (effect.modifier is ISkillModifier modifier)
                    {
                        AddModifier(modifier);
                    }
                }
                else
                {
                    Debug.LogWarning($"[ActiveSkillRuntime] 无法找到 effectId: {effectId}，可能效果池配置已更改");
                }
            }

            Debug.Log($"[ActiveSkillRuntime] 从存档恢复 {_chosenEvolutions.Count} 个进化效果，当前等级: {CurrentLevel}");
        }

        /// <summary>
        /// 获取当前等级（1 + 已选择进化效果数量，支持无限升级）
        /// </summary>
        public int CurrentLevel => 1 + ChosenEvolutions.Count;
    }
}