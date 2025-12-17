using System.Collections;
using UnityEngine;
using CardSystem.SkillSystem;

namespace CardSystem.SkillSystem.Targeting
{
    /// <summary>
    /// 抽象目标选择模块基类（ScriptableObject）。
    /// 责任：
    /// - 非交互式：通过 AcquireTargets 填充 ctx.Targets（实现应先 Clear）。
    /// - 交互式：将 RequiresManualSelection 设为 true，并实现 ManualSelectionCoroutine 来驱动玩家交互（高亮、点击等），
    ///              协程结束后需把所选目标写回 ctx.Targets 或 ctx.ExplicitTarget。
    ///
    /// 设计原则：
    /// - 保持轻量：AcquireTargets 返回匹配数量（方便调试/日志）。
    /// - 手动选择逻辑放在 Coroutine 中以便与 UI/输入系统解耦。
    /// </summary>
    public abstract class TargetingModuleSO : ScriptableObject
    {
        /// <summary>
        /// 若为 true，表示该模块需要玩家交互（例如点击某单位）才能确定目标。
        /// PlayerSkillComponent 将在 UseSkill 时运行 ManualSelectionCoroutine 并等待完成。
        /// </summary>
        public virtual bool RequiresManualSelection => false;

        /// <summary>
        /// 非交互式目标采集：实现应清空 ctx.Targets 并填充找到的目标，返回目标数量。
        /// aimPoint 可为 null（例如对于基于自身的 targeting）。
        /// </summary>
        /// <param name="ctx">技能上下文（Owner, Position, etc.）</param>
        /// <param name="aimPoint">可选的瞄点（通常来自鼠标世界坐标）</param>
        /// <returns>填充后的目标数量</returns>
        public abstract int AcquireTargets(SkillContext ctx, Vector3? aimPoint = null);

        /// <summary>
        /// 当 RequiresManualSelection==true 时可覆盖该协程以实现玩家交互选择（高亮、等待点击、取消等）。
        /// 协程结束后应把选择结果写入 ctx.Targets 或 ctx.ExplicitTarget。
        /// 默认实现直接结束（什么都不做）。
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public virtual IEnumerator ManualSelectionCoroutine(SkillContext ctx)
        {
            yield break;
        }

        #region Helpers

        /// <summary>
        /// 通用 helper：清空目标并返回 0（供模块实现复用）
        /// </summary>
        protected int ClearTargets(SkillContext ctx)
        {
            if (ctx == null) return 0;
            ctx.Targets.Clear();
            return 0;
        }

        #endregion
    }
}
