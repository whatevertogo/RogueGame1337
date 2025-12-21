using UnityEngine;

namespace Character.Interfaces
{
    public interface IDamageable
    {
        float TakeDamage(DamageInfo damageInfo);
        void TakeDamage(int amount);
    }

    public interface IHealable
    {
        void Heal(int amount);
        void Heal(float amount);
    }

    public interface ITeamMember
    {
        TeamType Team { get; }
    }

    /// <summary>
    /// 可击退接口
    /// </summary>
    public interface IKnockbackable
    {
        void ApplyKnockback(Vector2 direction, float force);
    }

    /// <summary>
    /// 玩家移动接口 - 解耦 RoomManager 和具体的移动组件
    /// </summary>
    public interface IPlayerMovement
    {
        void SetCanMove(bool canMove);
        void StopImmediately();
    }
}