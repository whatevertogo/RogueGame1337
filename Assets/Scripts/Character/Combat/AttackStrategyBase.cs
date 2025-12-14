using UnityEngine;

namespace Character.Combat
{
    /// <summary>
    /// 攻击策略 SO 基类 - 所有策略都继承此类
    /// </summary>
    public abstract class AttackStrategyBase : ScriptableObject, IAttackStrategy
    {
        [Header("基础配置")]
        [Tooltip("策略名称")]
        public string strategyName;

        [Tooltip("策略描述")]
        [TextArea(2, 4)]
        public string description;

        /// <summary>
        /// 执行攻击（子类实现）
        /// </summary>
        public abstract void Execute(AttackContext context);

        /// <summary>
        /// 在编辑器中绘制 Gizmos（可选重写）
        /// </summary>
        public virtual void DrawGizmos(Vector3 position, Vector2 direction) { }
    }
}