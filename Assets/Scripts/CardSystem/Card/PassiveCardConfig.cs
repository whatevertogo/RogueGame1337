
using System;
using UnityEngine;


[Serializable]
public class PassiveCardConfig
{
    //存储所有被动Effect的SO

    [Tooltip("被动效果列表")]
    [InlineEditor]
    public StatusEffectDefinitionSO[] passiveEffects;
}