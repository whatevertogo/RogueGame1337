using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueGame.Items
{
    /// <summary>
    /// 主动卡运行时状态（支持无限升级）
    /// 重构：添加进化效果 ID 列表用于保存
    /// </summary>
    [Serializable]
    public class ActiveCardState
    {
        public string CardId;            // 索引到 GameRoot.Instance.CardDataBase
        public string InstanceId;        // 可选：若卡牌不是全局唯一
        public bool IsEquipped;
        public int CurrentEnergy;
        public string EquippedPlayerId;  // null 表示在池中

        /// <summary>
        /// 当前技能等级（无上限，仅受软上限配置限制）
        /// </summary>
        public int Level = 1;

        /// <summary>
        /// 已选择的进化效果 ID 列表（用于保存）
        /// 注意：只保存 ID 字符串，不保存 ScriptableObject 引用
        /// 保存后在运行时通过 EvolutionEffectPool.GetEffectById() 恢复
        /// </summary>
        [SerializeField]
        private List<string> _chosenEffectIds = new();

        /// <summary>
        /// 获取已选择的效果 ID 列表（只读）
        /// </summary>
        public IReadOnlyList<string> ChosenEffectIds => _chosenEffectIds;

        /// <summary>
        /// 添加已选择的进化效果 ID
        /// </summary>
        /// <param name="effectId">效果唯一标识</param>
        public void AddChosenEffect(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                Debug.LogWarning("[ActiveCardState] 尝试添加空的 effectId");
                return;
            }
            _chosenEffectIds.Add(effectId);
        }

        /// <summary>
        /// 清空已选择的进化效果（用于重置）
        /// </summary>
        public void ClearChosenEffects()
        {
            _chosenEffectIds.Clear();
        }
    }
}
