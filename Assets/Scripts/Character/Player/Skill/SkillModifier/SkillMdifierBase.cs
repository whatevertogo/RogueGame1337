using UnityEngine;


public abstract class SkillModifierBase :ScriptableObject
{
    // ========== ISkillModifier 实现 ==========
    public abstract string ModifierId { get; }
}