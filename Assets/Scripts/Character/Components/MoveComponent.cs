using UnityEngine;
using Character.Components;
using Character.Interfaces;

namespace Character.Components
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovementComponent : MonoBehaviour, IPlayerMovement
    {
        private Rigidbody2D rb;
        private CharacterStats stats;
        private StatusEffectComponent statusEffects;

        private Vector2 inputDir;
        private Vector2 knockbackVelocity;
        private float knockbackTimer;

        [Header("调试"),ReadOnly]
        [SerializeField] private float currentSpeed;
        [ReadOnly,Tooltip("是否允许移动只能在代码里面改")]
        [SerializeField] private bool canMove = true;

        public float CurrentSpeed => currentSpeed;
        public bool IsMoving => currentSpeed > 0.1f;
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stats = GetComponent<CharacterStats>();
            statusEffects = GetComponent<StatusEffectComponent>();
        }

        private void FixedUpdate()
        {
            // 击退处理
            if (knockbackTimer > 0)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                rb.velocity = knockbackVelocity;
                return;
            }

            // 控制效果检查
            if (!canMove || (statusEffects != null && (statusEffects.IsStunned || statusEffects.IsRooted)))
            {
                rb.velocity = Vector2.zero;
                currentSpeed = 0;
                return;
            }

            // 正常移动
            float targetSpeed = stats?.MoveSpeed.Value ?? 4f;
            float accel = stats?.Acceleration.Value ?? 10f;

            Vector2 desiredVelocity = inputDir.normalized * targetSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, accel * Time.fixedDeltaTime);

            currentSpeed = rb.velocity.magnitude;

            // 更新朝向
            if (inputDir.sqrMagnitude > 0.01f)
            {
                FacingDirection = inputDir.normalized;
            }
        }

        public void SetInput(Vector2 dir)
        {
            inputDir = dir;
        }

        public void SetCanMove(bool value)
        {
            canMove = value;
            if (!value)
            {
                rb.velocity = Vector2.zero;
            }
        }

        public void ApplyKnockback(Vector2 direction, float force, float duration = 0.2f)
        {
            knockbackVelocity = direction.normalized * force;
            knockbackTimer = duration;
        }

        public void StopImmediately()
        {
            rb.velocity = Vector2.zero;
            inputDir = Vector2.zero;
            knockbackTimer = 0;
        }
    }
}