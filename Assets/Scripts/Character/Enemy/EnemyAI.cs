using UnityEngine;
using Character.Components;

/// <summary>
/// 简易敌人 AI：追逐最近的玩家并在接近时使用 CombatComponent 攻击。
/// 仅用于最小流程验证，后续可替换为更复杂的行为树/状态机。
/// </summary>
[RequireComponent(typeof(MovementComponent))]
[RequireComponent(typeof(Character.Components.CombatComponent))]
public class EnemyAI : MonoBehaviour
{
    private MovementComponent movement;
    private Character.Components.CombatComponent combat;
    private Character.Components.CharacterStats stats;
    private Transform target;

    [Header("AI 设置")]
    [Tooltip("当找不到玩家时的巡逻速度（可为0）")]
    public float idleSpeed = 0f;

    [Tooltip("寻找玩家的最大距离，超过将不追逐（0 表示无限）")]
    public float aggroRange = 0f;

    private void Awake()
    {
        movement = GetComponent<MovementComponent>();
        combat = GetComponent<Character.Components.CombatComponent>();
        stats = GetComponent<Character.Components.CharacterStats>();
    }

    private void Start()
    {
        // 优先使用场景中的本地玩家（单人场景）
        var p = FindObjectOfType<PlayerController>();
        if (p != null) target = p.transform;
    }

    private void Update()
    {
        if (stats == null || movement == null || combat == null) return;

        // 尝试动态查找玩家（容错）
        if (target == null)
        {
            var p = FindObjectOfType<PlayerController>();
            if (p != null) target = p.transform;
        }

        if (target == null)
        {
            // 无玩家：闲置或轻微徘徊
            movement.SetInput(Vector2.zero);
            return;
        }

        Vector2 toPlayer = (target.position - transform.position);
        float dist = toPlayer.magnitude;

        // 距离判断：若设置了 aggroRange 且超过则不追
        if (aggroRange > 0f && dist > aggroRange)
        {
            movement.SetInput(Vector2.zero);
            return;
        }

        float attackRange = stats.AttackRange?.Value ?? 1.5f;

        // 如果在攻击范围内，停止移动并攻击；否则移动靠近
        if (dist <= attackRange)
        {
            movement.SetInput(Vector2.zero);
            Vector2 aim = toPlayer.normalized;
            combat.SetAim(aim);
            // 尝试发起攻击（CombatComponent 会处理冷却）
            combat.TryAttack();
        }
        else
        {
            Vector2 dir = toPlayer.normalized;
            movement.SetInput(dir);
            combat.SetAim(dir);
        }
    }
}
