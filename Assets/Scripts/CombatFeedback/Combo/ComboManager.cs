using System.Collections.Generic;
using CDTU.Utils;
using Core.Events;
using RogueGame.Events;
using UnityEngine;

public class ComboManager : Singleton<ComboManager>
{
    [SerializeField]
    private ComboConfigSO ComboConfigSO;
    private int _currentCombo = 0;
    private float _remainingTime = 0f;

    private Dictionary<ComboState, ComboTier> _tierDict = new Dictionary<ComboState, ComboTier>();

    private ComboTier _currentTier;

    protected override void Awake()
    {
        base.Awake();

        // 安全检查：确保配置已赋值
        if (ComboConfigSO == null)
        {
            Debug.LogError("[ComboManager] ComboConfigSO 未配置！请在 Inspector 中分配 ComboConfigSO 资源。连击系统将无法正常工作。");
            return;
        }

        _remainingTime = ComboConfigSO.comboWindow;

        // 构建档位字典（按 ComboState 索引）
        foreach (var tier in ComboConfigSO.tiers)
        {
            _tierDict[tier.comboState] = tier;
        }

        // 初始化为 None 档位（无连击状态）
        _currentTier = new ComboTier
        {
            comboState = ComboState.None,
            threshold = 0,
            energyMult = 0,
            speedBonus = 0,
            rangeBonus = 0,
            tierColor = Color.white
        };

        Debug.Log("[ComboManager] 初始化完成，档位数量: " + ComboConfigSO.tiers.Count);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EntityKilledEvent>(OnEntityKilled);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EntityKilledEvent>(OnEntityKilled);
    }

    private void OnEntityKilled(EntityKilledEvent evt)
    {
        _currentCombo++;
        _remainingTime = ComboConfigSO.comboWindow;

        Debug.Log($"[ComboManager] 击杀事件: Combo={_currentCombo}, Victim={evt.Victim?.name}, Attacker={evt.Attacker?.name}");

        // 根据连击数获取当前档位（从后往前找第一个 threshold <= currentCombo 的档位）
        ComboTier nextTier = GetTierByComboCount(_currentCombo);

        if (nextTier != _currentTier)
        {
            _currentTier = nextTier;
            // 发布档位变化事件
            EventBus.Publish(new ComboTierChangedEvent(_currentTier));
        }

        // 发布连击变化事件
        EventBus.Publish(new ComboChangedEvent(_currentCombo, _currentTier));
    }

    /// <summary>
    /// 根据连击数获取对应档位（从后往前查找）
    /// </summary>
    private ComboTier GetTierByComboCount(int comboCount)
    {
        if (ComboConfigSO.tiers == null || ComboConfigSO.tiers.Count == 0)
            return default;

        // 特殊处理：comboCount = 0 时返回 None 档位
        if (comboCount <= 0)
        {
            return new ComboTier
            {
                comboState = ComboState.None,
                threshold = 0,
                energyMult = 0,
                speedBonus = 0,
                rangeBonus = 0,
                tierColor = Color.white
            };
        }

        // 从后往前找，找到第一个满足 threshold <= comboCount 的档位
        for (int i = ComboConfigSO.tiers.Count - 1; i >= 0; i--)
        {
            if (comboCount >= ComboConfigSO.tiers[i].threshold)
                return ComboConfigSO.tiers[i];
        }

        // 如果都没有匹配，返回第一个档位
        return ComboConfigSO.tiers[0];
    }

    private void Update()
    {
        // 只在有连击时递减时间
        if (_currentCombo > 0)
        {
            _remainingTime -= Time.deltaTime;

            // 超时重置
            if (_remainingTime <= 0)
            {
                ResetCombo();
            }
        }
    }

    /// <summary>
    /// 重置连击（超时中断）
    /// </summary>
    private void ResetCombo()
    {
        if (_currentCombo > 0)
        {
            // 发布连击中断事件（用于 UI 显示中断特效）
            EventBus.Publish(new ComboExpiredEvent(_currentCombo, (int)_currentTier.comboState));
        }

        // 重置状态
        _currentCombo = 0;
        _remainingTime = 0f;

        // 重置为 None 档位（无连击状态）
        _currentTier = new ComboTier
        {
            comboState = ComboState.None,
            threshold = 0,
            energyMult = 0,
            speedBonus = 0,
            rangeBonus = 0,
            tierColor = Color.white
        };

        // 发布连击归零事件
        EventBus.Publish(new ComboChangedEvent(0, _currentTier));
    }
}
