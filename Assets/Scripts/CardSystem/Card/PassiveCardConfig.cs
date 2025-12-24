
using System;
using UnityEngine;

/// <summary>
/// 被动卡牌配置
/// </summary>
[Serializable]
public class PassiveCardConfig
{
    [Tooltip("被动效果列表")]
    //
    public StatusEffectDefinitionSO[] passiveEffects;
    
    
    [Tooltip("是否可堆叠（拥有多张相同卡牌时）")]
    public bool stackable = true;
    
}

