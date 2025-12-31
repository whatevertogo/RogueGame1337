using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using UnityEngine;

/// <summary>
/// 冷却修改器：修改技能的有效冷却时间
/// 实现 ICooldownModifier 接口，在技能使用后应用
/// </summary>
public class CooldownModifier : ICooldownModifier
{
    // ========== 配置属性 ==========
    /// <summary>
    /// 冷却倍率（0.5 表示冷却减半，2.0 表示冷却翻倍）
    /// </summary>
    public float CooldownMultiplier { get; private set; }

    // ========== ISkillModifier 实现 ==========
    public string ModifierId => $"Cooldown({CooldownMultiplier}x)";

    // ========== 构造函数 ==========

    /// <summary>
    /// 创建冷却修改器
    /// </summary>
    /// <param name="cooldownMultiplier">冷却倍率（0.5 表示冷却减半，2.0 表示冷却翻倍）</param>
    public CooldownModifier(float cooldownMultiplier)
    {
        CooldownMultiplier = Mathf.Clamp(cooldownMultiplier, 0.1f, 10f);
    }

    // ========== 工厂方法 ==========

    /// <summary>
    /// 创建冷却缩减修改器
    /// </summary>
    /// <param name="reductionPercent">缩减百分比（0.2 表示缩减20%，即冷却变为原来的80%）</param>
    public static CooldownModifier Reduction(float reductionPercent)
    {
        return new CooldownModifier(1f - reductionPercent);
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
