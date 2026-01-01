using UnityEngine;

/// <summary>
/// 伤害计算结果（独立结构体，用于 DamageModifier）
/// </summary>
public struct DamageResult
{
    /// <summary>
    /// 是否为真实伤害（无视防御）
    /// </summary>
    public bool IsTrueDamage;

    /// <summary>
    /// 最终伤害值（由修改器计算后传递到效果系统）
    /// </summary>
    public float FinalDamage;

    /// <summary>
    /// 伤害来源对象（例如施放者或子弹的 GameObject）
    /// 注意：DamageResult 为 struct（值类型），如果作为参数传递并希望此计算结果生效，
    /// 请确保使用 ref 传递，或在调用后使用返回的结构体实例而不是原始拷贝。
    public GameObject Source;
    /// <summary>
    /// 初始化默认值
    /// </summary>
    public static DamageResult Default => new DamageResult
    {
        IsTrueDamage = false,
        FinalDamage = 0f,
        Source = null
    };
}
