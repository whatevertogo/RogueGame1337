using System;
using UnityEngine;

namespace Character.Components
{
    /// <summary>
    /// 角色属性组件 - 唯一数值源
    /// </summary>
    public class CharacterStats : MonoBehaviour
    {
        [Header("配置模板（必填）")]
        //
        [SerializeField] private CharacterStatsSO baseStatsSO;

        public Sprite Icon => baseStatsSO != null ? baseStatsSO.icon : null;

        [Header("═══ 运行时数据（只读）═══")]

        [Header("生命")]
        [ReadOnly][SerializeField] private Stat _maxHP = new(100);
        //生命恢复
        [ReadOnly][SerializeField] private Stat _hpRegen = new(0);
        [ReadOnly][SerializeField] private float _currentHP = 100;
        // 初始化期间抑制 HP 相关事件（避免 Awake/Initialize 触发订阅前的回调）
        private bool _suppressHPEvents = false;

        [Header("移动")]
        [ReadOnly][SerializeField] private Stat _moveSpeed = new(4f);
        [ReadOnly][SerializeField] private Stat _acceleration = new(10f);

        [Header("攻击")]
        [ReadOnly][SerializeField] private Stat _attackPower = new(10f);
        [ReadOnly][SerializeField] private Stat _attackSpeed = new(1f);
        [ReadOnly][SerializeField] private Stat _attackRange = new(1.5f);

        [Header("防御")]
        [ReadOnly][SerializeField] private Stat _armor = new(0f);
        [ReadOnly][SerializeField][Range(0f, 1f)] private Stat _dodge = new(0f);//闪避

        [Header("技能冷却速率")]
        [ReadOnly][SerializeField][Range(0f, 1f)] private Stat _skillCooldownReductionRate = new(0f);

        // ========== 属性访问器 ==========
        public Stat MaxHP => _maxHP;
        public Stat HPRegen => _hpRegen;
        public Stat MoveSpeed => _moveSpeed;
        public Stat Acceleration => _acceleration;
        public Stat AttackPower => _attackPower;
        public Stat AttackSpeed => _attackSpeed;
        public Stat AttackRange => _attackRange;
        // public Stat CritChance => _critChance;
        // public Stat CritDamage => _critDamage;
        public Stat Armor => _armor;
        //闪避
        public Stat Dodge => _dodge;
        public Stat SkillCooldownReductionRate => _skillCooldownReductionRate;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public float CurrentHP
        {
            get => _currentHP;
            set
            {
                float oldHP = _currentHP;
                _currentHP = Mathf.Clamp(value, 0, _maxHP.Value);

                float delta = _currentHP - oldHP;
                if (_suppressHPEvents) return;

                if (Math.Abs(delta) > 0.001f)
                {
                    if (delta < 0)
                    {
                        // 受伤（传递正数的伤害量）
                        OnDamaged?.Invoke(Mathf.Abs(delta));
                    }
                    else
                    {
                        // 治疗
                        OnHealed?.Invoke(delta);
                    }

                    // 总是通知生命值变化（current, max）
                    OnHealthChanged?.Invoke(_currentHP, _maxHP.Value);

                    // 如果从存活变为死亡，触发死亡事件
                    if (oldHP > 0f && _currentHP <= 0f)
                    {
                        OnDeath?.Invoke();
                    }
                }
            }
        }

        public bool IsDead => _currentHP <= 0;
        public float HPPercent => _maxHP.Value > 0 ? _currentHP / _maxHP.Value : 0;

        /// <summary>
        /// 获取原始配置 SO
        /// </summary>
        public CharacterStatsSO BaseSO => baseStatsSO;

        // ========== 事件 ==========
        public event Action<float, float> OnHealthChanged;  // (current, max)
        public event Action<float> OnDamaged;
        public event Action OnDeath;
        public event Action OnStatsChanged;
        public event Action<float> OnHealed;

        // ========== 生命周期 ==========

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (baseStatsSO != null)
            {
                InitializeFromSO(baseStatsSO);
            }
            else
            {
                CDTU.Utils.CDLogger.LogWarning($"[CharacterStats] {gameObject.name} 没有配置 CharacterStatsSO，使用默认值！");
            }

            // 应用属性上限配置
            ApplyStatLimits();

            // 初始化当前生命为满血
            if (_currentHP <= 0)
            {
                _currentHP = _maxHP.Value;
            }

            SubscribeToStatChanges();
        }

        /// <summary>
        /// 应用属性上限配置（从 GameRoot.StatLimitConfig 读取）
        /// </summary>
        private void ApplyStatLimits()
        {
            var config = GameRoot.Instance?.StatLimitConfig;
            if (config == null)
            {
                // 未配置上限，使用 Stat 类内部默认值（无限制）
                return;
            }

            // 应用各属性上限
            _maxHP.SetMaxValue(config.maxMaxHP);
            _hpRegen.SetMaxValue(config.maxHPRegen);
            _moveSpeed.SetMaxValue(config.maxMoveSpeed);
            _acceleration.SetMaxValue(config.maxAcceleration);
            _attackPower.SetMaxValue(config.maxAttackPower);
            _attackSpeed.SetMaxValue(config.maxAttackSpeed);
            _attackRange.SetMaxValue(config.maxAttackRange);
            _armor.SetMaxValue(config.maxArmor);
            _dodge.SetMaxValue(config.maxDodge);
            _skillCooldownReductionRate.SetMaxValue(config.maxSkillCooldownReduction);

            CDTU.Utils.CDLogger.Log($"[CharacterStats] {gameObject.name} 属性上限已应用");
        }

        private void InitializeFromSO(CharacterStatsSO so)
        {
            _maxHP.BaseValue = so.maxHP;
            _hpRegen.BaseValue = so.hpRegen;
            _moveSpeed.BaseValue = so.moveSpeed;
            _acceleration.BaseValue = so.acceleration;
            _attackPower.BaseValue = so.attackPower;
            _attackSpeed.BaseValue = so.attackSpeed;
            _attackRange.BaseValue = so.attackRange;
            // _critChance.BaseValue = so.critChance;
            // _critDamage.BaseValue = so.critDamage;
            _armor.BaseValue = so.armor;
            _dodge.BaseValue = so.dodge;
            _skillCooldownReductionRate.BaseValue = so.skillCooldownReductionRate;
        }

        private void SubscribeToStatChanges()
        {
            _maxHP.OnValueChanged += HandleStatChanged;
            _hpRegen.OnValueChanged += HandleStatChanged;
            _moveSpeed.OnValueChanged += HandleStatChanged;
            _acceleration.OnValueChanged += HandleStatChanged;
            _attackPower.OnValueChanged += HandleStatChanged;
            _attackSpeed.OnValueChanged += HandleStatChanged;
            _attackRange.OnValueChanged += HandleStatChanged;
            // _critChance.OnValueChanged += HandleStatChanged;
            // _critDamage.OnValueChanged += HandleStatChanged;
            _armor.OnValueChanged += HandleStatChanged;
            _dodge.OnValueChanged += HandleStatChanged;
            _skillCooldownReductionRate.OnValueChanged += HandleStatChanged;
        }

        private void HandleStatChanged()
        {
            OnStatsChanged?.Invoke();
        }

        // ========== 伤害系统 ==========

        /// <summary>
        /// 简化版受伤
        /// </summary>
        public DamageResult TakeDamage(float amount)
        {
            var info = DamageInfo.Create(amount);
            return TakeDamage(info);
        }
        /// <summary>
        /// 受到伤害
        /// </summary>
        public DamageResult TakeDamage(DamageInfo info)
        {

            if (IsDead) return new DamageResult(0, info.Source?.name ?? "Unknown");

            // 闪避判定（_dodge.Value 已经包含了上限限制）
            float actualDodge = _dodge.Value; // Stat 类会自动应用上限
            if (UnityEngine.Random.value < actualDodge)
            {
                return new DamageResult(0, info.Source?.name ?? "Unknown");
            }

            // 计算伤害
            float damage = info.Amount;

            // 应用护甲减伤
            damage = ApplyArmor(damage);

            // 应用状态效果对受到伤害的修改（例如伤害减免 buff）
            var effectComp = GetComponent<StatusEffectComponent>();
            if (effectComp != null)
            {
                damage = effectComp.ModifyIncomingDamage(damage);
            }

            // 最终伤害（至少1点）
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            CurrentHP -= finalDamage;

            return new DamageResult(finalDamage, info.Source?.name ?? "Unknown");
        }


        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHP += amount;
        }

        public void Heal(int amount) => Heal((float)amount);

        /// <summary>
        /// 完全恢复
        /// </summary>
        public void FullHeal()
        {
            CurrentHP = _maxHP.Value;
        }

        // ========== 攻击计算 ==========

        /// <summary>
        /// 计算攻击伤害
        /// </summary>
        public DamageInfo CalculateAttackDamage()
        {
            float damage = _attackPower.Value;

            return new DamageInfo
            {
                Amount = damage,
                // IsCrit = false,
                Source = gameObject
            };
        }

        /// <summary>
        /// 获取最终攻击力数值
        /// </summary>
        public float CalculateFinalAttack()
        {
            return CalculateAttackDamage().Amount;
        }

        // ========== 辅助方法 ==========

        /// <summary>
        /// 应用护甲减伤
        /// 公式：减伤率 = armor / (armor + 100)
        /// </summary>
        private float ApplyArmor(float damage)
        {
            float armor = _armor.Value;
            if (armor <= 0) return damage;

            float reduction = armor / (armor + 100f);
            return damage * (1f - reduction);
        }

        // ========== Stat 访问 ==========

        /// <summary>
        /// 根据类型获取 Stat
        /// </summary>
        public Stat GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.MaxHP => _maxHP,
                StatType.HPRegen => _hpRegen,
                StatType.MoveSpeed => _moveSpeed,
                StatType.Acceleration => _acceleration,
                StatType.AttackPower => _attackPower,
                StatType.AttackSpeed => _attackSpeed,
                StatType.AttackRange => _attackRange,
                // StatType.CritChance => _critChance,
                // StatType.CritDamage => _critDamage,
                StatType.Armor => _armor,
                StatType.Dodge => _dodge,
                _ => null
            };
        }

        /// <summary>
        /// 移除某来源的所有修饰符
        /// </summary>
        public void RemoveAllModifiersFromSource(object source)
        {
            _maxHP.RemoveAllModifiersFromSource(source);
            _hpRegen.RemoveAllModifiersFromSource(source);
            _moveSpeed.RemoveAllModifiersFromSource(source);
            _acceleration.RemoveAllModifiersFromSource(source);
            _attackPower.RemoveAllModifiersFromSource(source);
            _attackSpeed.RemoveAllModifiersFromSource(source);
            _attackRange.RemoveAllModifiersFromSource(source);
            // _critChance.RemoveAllModifiersFromSource(source);
            // _critDamage.RemoveAllModifiersFromSource(source);
            _armor.RemoveAllModifiersFromSource(source);
            _dodge.RemoveAllModifiersFromSource(source);
            _skillCooldownReductionRate.RemoveAllModifiersFromSource(source);
        }

        /// <summary>
        /// 清除所有修饰符
        /// </summary>
        public void ClearAllModifiers()
        {
            _maxHP.ClearAllModifiers();
            _hpRegen.ClearAllModifiers();
            _moveSpeed.ClearAllModifiers();
            _acceleration.ClearAllModifiers();
            _attackPower.ClearAllModifiers();
            _attackSpeed.ClearAllModifiers();
            _attackRange.ClearAllModifiers();
            // _critChance.ClearAllModifiers();
            // _critDamage.ClearAllModifiers();
            _armor.ClearAllModifiers();
            _dodge.ClearAllModifiers();
            _skillCooldownReductionRate.ClearAllModifiers();
        }
    }
}