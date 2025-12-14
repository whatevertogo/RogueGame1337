using Character.Components;
using Character.Core;

namespace Character.Effects
{
    /// <summary>
    /// 状态效果接口
    /// </summary>
    public interface IStatusEffect
    {
        string EffectId { get; }
        bool IsStackable { get; }

        void OnApply(CharacterStats stats, StatusEffectComponent comp);
        void OnTick(float deltaTime);
        void OnRemove(CharacterStats stats, StatusEffectComponent comp);

        bool IsExpired { get; }

        // 可选：修改输出伤害
        float ModifyOutgoingDamage(float damage) => damage;
        // 可选：修改受到伤害
        float ModifyIncomingDamage(float damage) => damage;
    }
}