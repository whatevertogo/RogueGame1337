using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using UnityEngine;

/// <summary>
/// 冷却修改器：修改技能的有效冷却时间
/// 实现 ICooldownModifier 接口，在技能使用后应用
/// </summary>
[CreateAssetMenu(fileName = "CooldownModifier", menuName = "RogueGame/Skill/Modifiers/CooldownModifier")]
public class CooldownModifier : SkillModifierBase, ICooldownModifier
{
    // ========== 配置属性 ==========
    /// <summary>
    /// 冷却倍率（0.5 表示冷却减半，2.0 表示冷却翻倍）
    /// </summary>
    public float CooldownMultiplier;

    // ========== ISkillModifier 实现 ==========
    public override string ModifierId => $"Cooldown({CooldownMultiplier}x)";

    // ========== 工厂方法（使用 ScriptableObject.CreateInstance） ==========
    /// <summary>
    /// 创建冷却缩减修改器
    /// </summary>
    public static CooldownModifier Reduction(float reductionPercent)
    {
        var inst = CreateInstance<CooldownModifier>();
        inst.CooldownMultiplier = Mathf.Clamp(1f - reductionPercent, 0.1f, 10f);
        return inst;
    }

    // ========== ICooldownModifier 实现 ==========

    /// <summary>
    /// 应用冷却修改（直接修改 runtime 的有效冷却时间）
    /// </summary>
    /// <param name="runtime">技能运行时数据</param>
    public void ApplyCooldown(ActiveSkillRuntime runtime)
    {
        if (runtime == null || runtime.Skill == null) return;

        // 计算并设置有效冷却时间
        runtime.EffectiveCooldown = runtime.Skill.cooldown * CooldownMultiplier;
    }
}
