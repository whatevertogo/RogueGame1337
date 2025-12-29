using System;
using RogueGame.Game.Service.SkillLimit;

namespace Character.Player
{
    /// <summary>
    /// 玩家技能限制器：提供意图型的技能限制接口
    /// 职责：根据房间规则判断技能是否可用，不负责技能释放逻辑
    /// 设计原则：面向接口/意图，不暴露内部状态细节
    /// </summary>
    public sealed class PlayerSkillLimiter
    {
        // 限制状态
        private bool _disableAll;
        private bool _noCooldown;
        private readonly bool[] _slotUsedInRoom = new bool[2]; // 追踪每个槽位在当前房间是否已使用

        /// <summary>
        /// 当前是否处于"禁用所有技能"状态
        /// </summary>
        public bool IsAllDisabled => _disableAll;

        /// <summary>
        /// 当前是否处于"无冷却"状态
        /// </summary>
        public bool IsNoCooldown => _noCooldown;

        /// <summary>
        /// 禁用所有技能
        /// </summary>
        public void DisableAll()
        {
            _disableAll = true;
        }


        /// <summary>
        /// 设置无冷却模式（测试用）
        /// </summary>
        public void SetNoCooldownMode()
        {
            _noCooldown = true;
        }

        /// <summary>
        /// 清除所有限制
        /// </summary>
        public void Clear()
        {
            _disableAll = false;
            _noCooldown = false;

            // 清除房间使用记录
            Array.Clear(_slotUsedInRoom, 0, _slotUsedInRoom.Length);
        }

        /// <summary>
        /// 检查指定槽位的技能是否可用
        /// 这是核心接口，技能释放前调用此方法判断
        /// </summary>
        /// <param name="slotIndex">槽位索引（0=Q, 1=E）</param>
        /// <returns>true 表示可以使用，false 表示禁止使用</returns>
        public bool CanUseSkill(int slotIndex)
        {
            // 检查 1: 全局禁用
            if (_disableAll)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 标记指定槽位的技能已使用（技能成功释放后调用）
        /// </summary>
        /// <param name="slotIndex">槽位索引（0=Q, 1=E）</param>
        public void MarkSkillUsed(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slotUsedInRoom.Length)
            {
                _slotUsedInRoom[slotIndex] = true;
            }
        }

        /// <summary>
        /// 检查指定槽位的技能在当前房间是否已使用
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <returns>true 表示已使用，false 表示未使用</returns>
        public bool IsUsedInCurrentRoom(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotUsedInRoom.Length)
                return false;

            return _slotUsedInRoom[slotIndex];
        }

        /// <summary>
        /// 获取限制状态的描述（用于调试和 UI 显示）
        /// </summary>
        public string GetLimitDescription()
        {
            if (_disableAll)
                return "所有技能已禁用";

            if (_noCooldown)
                return "无冷却模式";

            return "无限制";
        }
    }
}
