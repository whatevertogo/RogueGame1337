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
        public int Level = 1;            // 技能等级（Lv1-Lv5），用于主动技能升级系统
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
            /// <summary>进化到的等级</summary>
            public int Level;

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
        public void AddChoice(int level, bool choseBranchA, string branchId)
        {
            Choices.Add(new EvolutionChoice
            {
                Level = level,
                ChoseBranchA = choseBranchA,
                BranchId = branchId
            });
        }
    }
}
