using Character.Components;

namespace Character.Effects
{
    /// <summary>
    /// 状态效果接口
    /// </summary>
    public interface IStatusEffect
    {
        string EffectId { get; }
        // 是否可叠加
        bool IsStackable { get; }

        void OnApply(CharacterStats stats, StatusEffectComponent comp);
        void OnTick(float deltaTime);
        void OnRemove(CharacterStats stats, StatusEffectComponent comp);

        // 是否过期
        bool IsExpired { get; }

        // 可选：修改输出伤害
        float ModifyOutgoingDamage(float damage) => damage;
        // 可选：修改受到伤害
        float ModifyIncomingDamage(float damage) => damage;

        /// <summary>
        /// 刷新效果（例如重置持续时间）。建议所有运行时实例实现此方法。
        /// </summary>
        void Refresh();

        /// <summary>
        /// 延长效果持续时间
        /// </summary>
        /// <param name="extraTime">额外时间（秒）</param>
        void Extend(float extraTime);
    }
}