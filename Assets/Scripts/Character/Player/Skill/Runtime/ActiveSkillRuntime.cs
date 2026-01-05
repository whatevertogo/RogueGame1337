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

        // ========== 进化状态（阶段3使用） ==========
        /// <summary>
        /// 当前进化节点（Lv2-Lv5）
        /// </summary>
        [NonSerialized]
        public SkillNode CurrentNode;

        /// <summary>
        /// 分支选择历史（记录路径，如 "A-A-B-A"）
        /// </summary>
        [NonSerialized]
        public List<SkillBranch> BranchHistory = new List<SkillBranch>();

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

        // ========== 进化管理 ==========
        /// <summary>
        /// 设置进化节点并应用分支修改器
        /// </summary>
        public void SetEvolutionNode(SkillNode node, SkillBranch selectedBranch)
        {
            if (node == null || selectedBranch == null) return;

            CurrentNode = node;
            BranchHistory.Add(selectedBranch);

            // 应用分支的修改器（包括效果生成修改器）
            if (selectedBranch.modifiers != null)
            {
                foreach (var modifier in selectedBranch.modifiers)
                {
                    if (modifier is ISkillModifier skillModifier)
                    {
                        AddModifier(skillModifier);
                    }
                }
            }

            // 修改器已添加，效果缓存会在 AddModifier 中自动标记为脏
        }

        /// <summary>
        /// 获取当前等级（1 + 进化历史数量）
        /// </summary>
        public int CurrentLevel => 1 + BranchHistory.Count;
    }
}