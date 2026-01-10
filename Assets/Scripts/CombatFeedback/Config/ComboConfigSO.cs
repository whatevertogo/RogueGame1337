using System;
using System.Collections.Generic;
using UnityEngine;

[ManagedData("ComboConfigSO")]
[CreateAssetMenu(fileName = "ComboConfig", menuName = "RogueGame/Config/ComboConfig")]
public class ComboConfigSO : ScriptableObject
{
    [Header("基本设置")]
    [Tooltip("连击持续时间（秒）")]
    public float comboWindow = 3f; // 连击持续时间

    [Tooltip("剩余几秒时 UI 开始闪烁警告")]
    public float warningThreshold = 1f; // 剩余几秒时 UI 开始闪烁警告

    [Header("档位定义")]
    [Tooltip("连击档位列表，按阈值升序排列")]
    public List<ComboTier> tiers = new List<ComboTier>();
}
