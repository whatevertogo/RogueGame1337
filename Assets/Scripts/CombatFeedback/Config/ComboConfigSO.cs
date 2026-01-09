using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboConfig", menuName = "RogueGame/Config/ComboConfig")]
public class ComboConfigSO : ScriptableObject
{
    [Header("Basic Settings")]
    public float comboWindow = 3f; // 连击持续时间
    public float warningThreshold = 1f; // 剩余几秒时 UI 开始闪烁警告

    [Header("Tier Definitions")]
    public List<ComboTier> tiers = new List<ComboTier>();

    // 根据连击数获取当前档位数据
    public ComboTier GetTier(int comboCount)
    {
        // 从后往前找，找到第一个小于等于当前连击数的档位
        for (int i = tiers.Count - 1; i >= 0; i--)
        {
            if (comboCount >= tiers[i].threshold)
                return tiers[i];
        }
        return tiers[0];
    }
}

[Serializable]
public struct ComboTier
{
    [Tooltip("档位名称（普通、狂热、屠戮、毁灭等）")]
    public string tierName;

    [Tooltip("触发此档位所需的最少连击数")]
    public int threshold;

    [Tooltip("连击持续时间增加（秒）")]
    public float energyMult;

    [Tooltip("移动速度加成（0.15 = +15%）")]
    public float speedBonus;

    [Tooltip("攻击范围加成（0.2 = +20%）")]
    public float rangeBonus;

    [Tooltip("此档位的 UI 和特效显示颜色")]
    public Color tierColor;
}
