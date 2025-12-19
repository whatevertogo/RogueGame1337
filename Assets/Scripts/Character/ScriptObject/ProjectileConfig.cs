using UnityEngine;

/// <summary>
/// 投射物配置
/// </summary>
[CreateAssetMenu(fileName = "ProjectileConfig", menuName = "RogueGame/Projectile Config")]
public class ProjectileConfig : ScriptableObject
{
    [Header("预制体")]
    [Tooltip("投射物预制体，必须包含 ProjectileBase 组件")]
    public GameObject projectilePrefab;

    [Header("运动属性")]
    [Tooltip("飞行速度")]
    public float speed = 12f;

    [Tooltip("存活时间（秒）")]
    public float lifetime = 3f;

    [Tooltip("穿透次数（0=不穿透）")]
    public int pierceCount = 0;

    [Header("伤害")]
    [Tooltip("伤害倍率（最终伤害 = 攻击力 × 此倍率）")]
    public float damageMultiplier = 1f;

    [Header("追踪（可选）")]
    [Tooltip("是否追踪目标")]
    public bool isHoming = false;

    [Tooltip("追踪转向强度")]
    public float homingStrength = 5f;

    [Tooltip("追踪搜索半径")]
    public float homingRadius = 6f;

    [Header("特效（可选）")]
    [Tooltip("命中特效")]
    public GameObject hitEffect;

    [Tooltip("拖尾特效")]
    public GameObject trailEffect;

    [Header("碰撞检测")]
    [Tooltip("碰撞检测图层掩码")]
    public LayerMask hitMask = -1; // 默认检测所有图层
}