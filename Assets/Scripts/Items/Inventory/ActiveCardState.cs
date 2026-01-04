using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueGame.Items
{
    /// <summary>
    /// 主动卡运行时状态（纯充能系统，无CD）
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
        /// 技能进化历史记录（结构化存储，避免字符串硬编码）
        /// </summary>
        [SerializeField]
        private SkillEvolutionHistory _evolutionHistory = new();

        /// <summary>
        /// 获取进化历史记录
        /// </summary>
        public SkillEvolutionHistory EvolutionHistory => _evolutionHistory;

        /// <summary>
        /// 当前等级（1 + 进化选择次数），用进化历史计算，避免重复存储
        /// </summary>
        public int Level => (_evolutionHistory?.Choices?.Count ?? 0) + 1;
    }

    /// <summary>
    /// 技能进化历史记录
    /// </summary>
    [Serializable]
    public class SkillEvolutionHistory
    {
        /// <summary>
        /// 所有的进化选择记录
        /// </summary>
        public List<EvolutionChoice> Choices = new();

        /// <summary>
        /// 单次进化选择记录
        /// </summary>
        [Serializable]
        public class EvolutionChoice
        {
            /// <summary>true=分支A, false=分支B</summary>
            public bool ChoseBranchA;

            /// <summary>分支唯一标识（用于回溯）</summary>
            public string BranchId;
        }

        /// <summary>
        /// 获取分支路径字符串（例如 "A-B-A-B"）
        /// </summary>
        public string GetPathString()
        {
            if (Choices.Count == 0) return string.Empty;
            return string.Join("-", Choices.Select(c => c.ChoseBranchA ? "A" : "B"));
        }

        /// <summary>
        /// 添加新的进化选择
        /// </summary>
        public void AddChoice(bool choseBranchA, string branchId)
        {
            Choices.Add(new EvolutionChoice
            {
                ChoseBranchA = choseBranchA,
                BranchId = branchId
            });
        }
    }
}
