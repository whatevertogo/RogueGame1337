using System.Collections.Generic;
using System.Linq;
using RogueGame.Items;

namespace RogueGame.SaveSystem
{
    /// <summary>
    /// 运行态与存档态数据转换扩展方法
    /// 消除手动拷贝代码，减少出错风险
    /// </summary>
    public static class SaveDataExtensions
    {
        /// <summary>
        /// 将运行时卡牌状态转换为存档数据
        /// </summary>
        public static ActiveCardSaveData ToSaveData(this ActiveCardState state)
        {
            if (state == null) return null;

            return new ActiveCardSaveData
            {
                CardId = state.CardId,
                InstanceId = state.InstanceId,
                CurrentCharges = state.CurrentCharges,
                IsEquipped = state.IsEquipped,
                EquippedPlayerId = state.EquippedPlayerId,
                Level = state.Level  // 保存技能等级
            };
        }

        /// <summary>
        /// 批量转换运行时卡牌状态列表为存档数据列表
        /// </summary>
        public static List<ActiveCardSaveData> ToSaveDataList(this IEnumerable<ActiveCardState> states)
        {
            if (states == null) return new List<ActiveCardSaveData>();
            return states.Select(s => s.ToSaveData()).Where(s => s != null).ToList();
        }

        /// <summary>
        /// 将存档数据恢复为运行时卡牌状态
        /// </summary>
        public static ActiveCardState ToRuntimeState(this ActiveCardSaveData saveData)
        {
            if (saveData == null) return null;

            return new ActiveCardState
            {
                CardId = saveData.CardId,
                InstanceId = saveData.InstanceId,
                CurrentCharges = saveData.CurrentCharges,
                IsEquipped = saveData.IsEquipped,
                EquippedPlayerId = saveData.EquippedPlayerId,
                Level = saveData.Level,  // 恢复技能等级
                CooldownRemaining = 0f // 运行时状态重置
            };
        }

        /// <summary>
        /// 批量转换存档数据列表为运行时状态列表
        /// </summary>
        public static List<ActiveCardState> ToRuntimeStateList(this IEnumerable<ActiveCardSaveData> saveDataList)
        {
            if (saveDataList == null) return new List<ActiveCardState>();
            return saveDataList.Select(s => s.ToRuntimeState()).Where(s => s != null).ToList();
        }
    }
}
