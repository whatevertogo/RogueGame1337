using UnityEngine;
using Character.Core;
using Character.Components;

namespace Character.Combat
{
    /// <summary>
    /// 近战攻击策略 - 扇形范围
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeAttack", menuName = "RogueGame/Combat/Melee Attack")]
    public class MeleeAttackStrategy : AttackStrategyBaseSO
    {
        [Header("近战配置")]
        [Tooltip("攻击半径")]
        public float radius = 1.8f;

        [Tooltip("扇形角度（度）")]
        [Range(10f, 360f)]
        public float arcAngle = 90f;

        [Tooltip("击退力度")]
        public float knockbackForce = 3f;

        [Header("偏移")]
        [Tooltip("攻击中心偏移（相对于角色位置）")]
        public float forwardOffset = 0.5f;

        [Header("特效")]
        public GameObject slashEffect;
        public float effectDuration = 0.5f;

        [Header("音效")]
        public AudioClip slashSound;

        public override void Execute(AttackContext context)
        {
            // 计算攻击中心点（向前偏移）
            Vector2 origin = (Vector2)context.FirePosition + context.AimDirection * forwardOffset;
            Vector2 direction = context.AimDirection;

            // 获取范围内的所有碰撞体
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, context.HitMask);
            int hitCount = 0;

            foreach (var hit in hits)
            {
                // 跳过自己
                if (hit.transform == context.Owner) continue;

                // 检查是否在扇形角度内
                Vector2 toTarget = ((Vector2)hit.transform.position - origin).normalized;
                float angle = Vector2.Angle(direction, toTarget);

                if (angle > arcAngle / 2f) continue;

                // 阵营检查
                var targetBase = hit.GetComponent<CharacterBase>();
                if (targetBase != null && targetBase.Team == context.OwnerTeam)
                    continue;

                // 造成伤害
                var health = hit.GetComponent<HealthComponent>();
                if (health != null)
                {
                    // 复制 DamageInfo 并设置击退
                    var damageInfo = context.DamageInfo;
                    damageInfo.KnockbackDir = toTarget;
                    damageInfo.KnockbackForce = knockbackForce;
                    damageInfo.HitPoint = hit.transform.position;

                    health.TakeDamage(damageInfo);
                    hitCount++;
                }
            }

            // 生成斩击特效
            SpawnSlashEffect(context);

            // 播放音效
            PlaySlashSound(context);

#if UNITY_EDITOR
            // 调试日志
            if (hitCount > 0)
            {
                Debug.Log($"[MeleeAttackStrategy] 命中 {hitCount} 个目标");
            }
#endif
        }

        private void SpawnSlashEffect(AttackContext context)
        {
            if (slashEffect == null) return;

            // 计算特效旋转角度
            float angle = Mathf.Atan2(context.AimDirection.y, context.AimDirection.x) * Mathf.Rad2Deg;

            // 计算特效位置（向前偏移）
            Vector3 effectPos = context.FirePosition + (Vector3)(context.AimDirection * forwardOffset);

            var effect = Instantiate(slashEffect, effectPos, Quaternion.Euler(0, 0, angle));
            Destroy(effect, effectDuration);
        }

        private void PlaySlashSound(AttackContext context)
        {
            if (slashSound == null) return;

            // 在攻击位置播放音效
            AudioSource.PlayClipAtPoint(slashSound, context.FirePosition);
        }

        public override void DrawGizmos(Vector3 position, Vector2 direction)
        {
            // 计算攻击中心
            Vector3 center = position + (Vector3)(direction * forwardOffset);

            // 绘制攻击范围圆
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(center, radius);

            // 绘制扇形边界
            Gizmos.color = Color.red;
            float halfAngle = arcAngle / 2f;

            Vector2 leftDir = RotateVector(direction, halfAngle);
            Vector2 rightDir = RotateVector(direction, -halfAngle);

            Gizmos.DrawRay(center, leftDir * radius);
            Gizmos.DrawRay(center, rightDir * radius);

            // 绘制扇形弧线（近似）
            int segments = 20;
            float angleStep = arcAngle / segments;
            Vector3 prevPoint = center + (Vector3)(rightDir * radius);

            for (int i = 1; i <= segments; i++)
            {
                float currentAngle = -halfAngle + angleStep * i;
                Vector2 dir = RotateVector(direction, currentAngle);
                Vector3 point = center + (Vector3)(dir * radius);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }

        /// <summary>
        /// 旋转向量
        /// </summary>
        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}