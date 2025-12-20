using System.Collections.Generic;
using UnityEngine;
using Character.Effects;
using System.Linq;

namespace Character.Components
{
    /// <summary>
    /// 状态效果管理组件
    /// </summary>
    public class StatusEffectComponent : MonoBehaviour
    {
        private readonly List<IStatusEffect> _effects = new();
        private readonly List<IStatusEffect> _pendingAdd = new();
        private readonly List<IStatusEffect> _pendingRemove = new();

        private CharacterStats stats;
        private bool _isUpdating;

        /// <summary>
        /// 是否处于眩晕状态
        /// </summary>
        public bool IsStunned { get; private set; }
        /// <summary>
        /// 是否处于沉默状态
        /// </summary>
        public bool IsSilenced { get; private set; }
        /// <summary>
        /// 是否处于定身状态
        /// </summary>
        public bool IsRooted { get; private set; }

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
        }

        private void Update()
        {
            _isUpdating = true;

            // 更新所有效果
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.OnTick(Time.deltaTime);

                if (effect.IsExpired)
                {
                    _pendingRemove.Add(effect);
                }
            }

            _isUpdating = false;

            // 优先处理待移除，避免在刷新非叠加效果时出现先添加后移除导致刷新失效的问题
            if (_pendingRemove.Count > 0)
            {
                foreach (var effect in _pendingRemove)
                {
                    RemoveEffectInternal(effect);
                }
                _pendingRemove.Clear();
            }

            // 处理待添加（去重后添加）
            if (_pendingAdd.Count > 0)
            {
                foreach (var effect in _pendingAdd)
                {
                    AddEffectInternal(effect);
                }
                _pendingAdd.Clear();
            }
        }

        /// <summary>
        /// 添加状态效果
        /// </summary>
        public void AddEffect(IStatusEffect effect)
        {
            if (_isUpdating)
            {
                if (!_pendingAdd.Contains(effect)) _pendingAdd.Add(effect);
            }
            else
            {
                AddEffectInternal(effect);
            }
        }

        /// <summary>
        /// 内部添加效果逻辑
        /// </summary>
        /// <param name="effect"></param>
        private void AddEffectInternal(IStatusEffect effect)
        {
            // 检查是否已存在同类效果
            var existing = _effects.Find(e => e.EffectId == effect.EffectId);

            if (existing != null)
            {
                // 使用已存在实例的叠加语义（以现有实例为准）
                if (existing.IsStackable)
                {
                    // 可叠加：添加新效果
                    _effects.Add(effect);
                    effect.OnApply(stats, this);
                }
                else
                {
                    // 不可叠加：刷新现有效果（通过接口调用）
                    existing.Refresh();
                }
            }
            else
            {
                _effects.Add(effect);
                effect.OnApply(stats, this);
            }
        }

        /// <summary>
        /// 移除状态效果
        /// </summary>
        public void RemoveEffect(IStatusEffect effect)
        {
            if (_isUpdating)
            {
                if (!_pendingRemove.Contains(effect)) _pendingRemove.Add(effect);
            }
            else
            {
                RemoveEffectInternal(effect);
            }
        }

        private void RemoveEffectInternal(IStatusEffect effect)
        {
            if (_effects.Remove(effect))
            {
                effect.OnRemove(stats, this);
            }
        }

        /// <summary>
        /// 移除指定 ID 的所有效果
        /// </summary>
        public void RemoveEffectsById(string effectId)
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].EffectId == effectId)
                {
                    RemoveEffect(_effects[i]);
                }
            }
        }

        /// <summary>
        /// 清除所有效果
        /// </summary>
        public void ClearAllEffects()
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                _effects[i].OnRemove(stats, this);
            }
            _effects.Clear();
        }

        /// <summary>
        /// 修改输出伤害
        /// </summary>
        public float ModifyOutgoingDamage(float damage)
        {
            foreach (var effect in _effects)
            {
                damage = effect.ModifyOutgoingDamage(damage);
            }
            return damage;
        }

        /// <summary>
        /// 修改受到伤害
        /// </summary>
        public float ModifyIncomingDamage(float damage)
        {
            foreach (var effect in _effects)
            {
                damage = effect.ModifyIncomingDamage(damage);
            }
            return damage;
        }

        /// <summary>
        /// 检查是否有指定效果
        /// </summary>
        public bool HasEffect(string effectId)
        {
            return _effects.Any(e => e.EffectId == effectId);
        }

        /// <summary>
        /// 获取指定效果的层数
        /// </summary>
        public int GetEffectStacks(string effectId)
        {
            int count = 0;
            foreach (var e in _effects)
            {
                if (e.EffectId == effectId) count++;
            }
            return count;
        }

        // 控制效果设置
        public void SetStunned(bool value) => IsStunned = value;
        public void SetSilenced(bool value) => IsSilenced = value;
        public void SetRooted(bool value) => IsRooted = value;
    }
}