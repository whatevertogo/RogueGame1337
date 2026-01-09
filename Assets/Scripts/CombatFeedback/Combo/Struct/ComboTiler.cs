using System;
using UnityEngine;

[Serializable]
public struct ComboTier
{
    [Tooltip("档位名称（普通、狂热、屠戮、毁灭等）")]
    public ComboState comboState;

    [Tooltip("触发此档位所需的最少连击数")]
    public int threshold;

    [Tooltip("连击持续时间增加（秒）")]
    public float energyMult;

    [Tooltip("移动速度加成")]
    public float speedBonus;

    [Tooltip("攻击范围加成")]
    public float rangeBonus;

    [Tooltip("此档位的 UI 和特效显示颜色")]
    public Color tierColor;
}
