
using System;
using System.Collections.Generic;
using Character.Player.Skill.Evolution;
using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Targeting;
using UnityEngine;

namespace Character.Player.Skill.Runtime
{
    /// <summary>
    /// 主动技能运行时状态：支持修改器和进化树
    /// </summary>
    [Serializable]
    public class ActiveSkillRuntime
    {
        // ========== 基础字段 ==========
        public string CardId;
        public SkillDefinition Skill;

        // 链接到 InventoryServiceManager 中的 ActiveCardState.instanceId
        public string InstanceId;
        public float LastUseTime;

        // 运行时协程引用（非序列化）用于取消延迟/选择流程
        [NonSerialized]
        public Coroutine RunningCoroutine;

        /// <summary>
        /// 实际消耗的能量值（用于协程取消时退还，包含修改器影响）
        /// </summary>
        [NonSerialized]
        public int ActualEnergyConsumed;


        /// <summary>
        /// 能量已消耗标志（用于协程取消时退还能量）
        /// </summary>
        [NonSerialized]
        public bool EnergyConsumed;
        /// <summary>
        /// 修改器列表脏标志（需要重新排序时设为 true）
        /// </summary>
        [NonSerialized]
        private bool _modifiersDirty;

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

        // ========== 动态属性（由修改器影响） ==========
        /// <summary>
        /// 有效冷却时间（基础冷却 × 修改器倍率）
        /// </summary>
        [NonSerialized]
        public float EffectiveCooldown;

        /// <summary>
        /// 弹射次数
        /// </summary>
        [NonSerialized]
        public int BounceCount;

        /// <summary>
        /// 弹射范围
        /// </summary>
        [NonSerialized]
        public float BounceRange = 5f;

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
            LastUseTime = -999f;

            // 初始化动态属性
            EffectiveCooldown = skill?.cooldown ?? 1f;
            BounceCount = 0;
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
            _modifiersDirty = true;
        }

        /// <summary>
        /// 移除指定修改器
        /// </summary>
        public bool RemoveModifier(ISkillModifier modifier)
        {
            return _activeModifiers.Remove(modifier);
        }

        /// <summary>
        /// 清除所有修改器
        /// </summary>
        public void ClearAllModifiers()
        {
            _activeModifiers.Clear();
        }

        /// <summary>
        /// 应用所有修改器到上下文
        /// </summary>
        public void ApplyAllModifiers(ref SkillTargetContext ctx)
        {
            // 惰性排序：仅在需要时排序
            if (_modifiersDirty)
            {
                _activeModifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _modifiersDirty = false;
            }

            foreach (var modifier in _activeModifiers)
            {
                try
                {
                    modifier.Apply(this, ref ctx);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ActiveSkillRuntime] 修改器 {modifier.GetType().Name} 应用失败: {ex.Message}");
                }
            }
        }

        #endregion
        // ========== 进化管理 ==========
        /// <summary>
        /// 设置进化节点并应用分支修改器
        /// </summary>
        public void SetEvolutionNode(SkillNode node, SkillBranch selectedBranch)
        {
            CurrentNode = node;
            BranchHistory.Add(selectedBranch);

            // 应用分支的修改器
            if (selectedBranch?.modifiers != null)
            {
                foreach (var modifier in selectedBranch.modifiers)
                {
                    if (modifier is ISkillModifier skillModifier)
                    {
                        AddModifier(skillModifier);
                    }
                }
            }

            // 重置有效冷却
            EffectiveCooldown = Skill?.cooldown ?? 1f;
        }

        /// <summary>
        /// 获取当前等级（1 + 进化历史数量）
        /// </summary>
        public int CurrentLevel => 1 + BranchHistory.Count;
    }
}