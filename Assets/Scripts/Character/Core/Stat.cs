using System;
using System.Collections.Generic;
using Character;
using UnityEngine;

    /// <summary>
    /// 单个属性，支持基础值 + 多个修饰符
    /// </summary>
    [Serializable]
    public class Stat
    {
        public event Action OnValueChanged;

        [SerializeField] private float _baseValue;
        private readonly List<StatModifier> _modifiers = new();

        private bool _isDirty = true;
        private float _cachedValue;
        private float _lastBaseValue;

        public float BaseValue
        {
            get => _baseValue;
            set
            {
                if (Math.Abs(_baseValue - value) > 0.0001f)
                {
                    _baseValue = value;
                    SetDirty();
                }
            }
        }

        public float Value
        {
            get
            {
                // 检测 Inspector 中的修改
                if (Math.Abs(_lastBaseValue - _baseValue) > 0.0001f)
                {
                    _lastBaseValue = _baseValue;
                    _isDirty = true;
                }

                if (_isDirty)
                {
                    _cachedValue = CalculateFinalValue();
                    _isDirty = false;
                }
                return _cachedValue;
            }
        }

        public Stat() { _lastBaseValue = _baseValue; }
        public Stat(float baseValue)
        {
            _baseValue = baseValue;
            _lastBaseValue = baseValue;
        }

        public void AddModifier(StatModifier mod)
        {
            _modifiers.Add(mod);
            _modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
            SetDirty();
        }

        public bool RemoveModifier(StatModifier mod)
        {
            if (_modifiers.Remove(mod))
            {
                SetDirty();
                return true;
            }
            return false;
        }

        public void RemoveAllModifiersFromSource(object source)
        {
            bool removed = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].Source == source)
                {
                    _modifiers.RemoveAt(i);
                    removed = true;
                }
            }
            if (removed) SetDirty();
        }

        public void ClearAllModifiers()
        {
            if (_modifiers.Count > 0)
            {
                _modifiers.Clear();
                SetDirty();
            }
        }

        public int GetModifierCount() => _modifiers.Count;

        private void SetDirty()
        {
            _isDirty = true;
            OnValueChanged?.Invoke();
        }

        /// <summary>
        /// 计算最终值
        /// 公式：(Base + FlatSum) × (1 + PercentAddSum) × PercentMult1 × PercentMult2 × ...
        /// </summary>
        private float CalculateFinalValue()
        {
            float finalValue = _baseValue;
            float percentAddSum = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var mod = _modifiers[i];
                switch (mod.Type)
                {
                    case StatModType.Flat:
                        finalValue += mod.Value;
                        break;
                    case StatModType.PercentAdd:
                        percentAddSum += mod.Value;
                        // 检查下一个是否还是 PercentAdd
                        if (i + 1 >= _modifiers.Count || _modifiers[i + 1].Type != StatModType.PercentAdd)
                        {
                            finalValue *= (1 + percentAddSum);
                            percentAddSum = 0;
                        }
                        break;
                    case StatModType.PercentMult:
                        finalValue *= (1 + mod.Value);
                        break;
                }
            }

            return (float)Math.Round(finalValue, 4);
        }

        // 隐式转换，方便使用
        public static implicit operator float(Stat stat) => stat.Value;
    }