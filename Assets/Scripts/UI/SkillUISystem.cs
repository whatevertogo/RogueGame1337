using UnityEngine;
using UnityEngine.UI;
using UI;
using Character.Skills;
using RogueGame.Map;

namespace Game.UI
{
    /// <summary>
    /// 技能UI系统 - 负责技能图标的加载和显示
    /// </summary>
    public class SkillUISystem
    {
        private PlayingStateUIView _uiView;

        public SkillUISystem(PlayingStateUIView uiView)
        {
            _uiView = uiView;
        }

        /// <summary>
        /// 更新技能槽图标显示
        /// </summary>
        /// <param name="slotIndex">技能槽索引 (0=Q, 1=E)</param>
        /// <param name="skillId">技能ID</param>
        public void UpdateSkillSlotIcon(int slotIndex, string skillId)
        {
            if (_uiView == null) return;

            // 尝试加载技能图标
            Sprite icon = LoadSkillIcon(skillId);
            _uiView.SetSkillSlotIcon(slotIndex, icon);
            
            Debug.Log($"[SkillUISystem] 更新技能槽 {slotIndex} 图标: {skillId} (图标: {icon?.name ?? "null"})");
        }

        /// <summary>
        /// 加载技能图标
        /// </summary>
        /// <param name="skillId">技能ID</param>
        /// <returns>技能图标Sprite</returns>
        private Sprite LoadSkillIcon(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;

            // 尝试多种路径加载技能图标
            string[] possiblePaths = {
                $"Sprites/Skills/{skillId}",
                $"Sprites/{skillId}",
                skillId
            };

            foreach (string path in possiblePaths)
            {
                Sprite icon = Resources.Load<Sprite>(path);
                if (icon != null)
                {
                    return icon;
                }
            }

            // 如果没有找到图标，返回默认图标
            Debug.LogWarning($"[SkillUISystem] 未找到技能图标: {skillId}，使用默认图标");
            return LoadDefaultSkillIcon();
        }

        /// <summary>
        /// 加载默认技能图标
        /// </summary>
        /// <returns>默认技能图标</returns>
        private Sprite LoadDefaultSkillIcon()
        {
            // 尝试加载默认图标
            Sprite defaultIcon = Resources.Load<Sprite>("Sprites/DefaultSkillIcon");
            if (defaultIcon != null)
            {
                return defaultIcon;
            }

            // 如果没有默认图标，返回null（UI组件会处理空图标显示）
            Debug.LogWarning("[SkillUISystem] 未找到默认技能图标");
            return null;
        }

        /// <summary>
        /// 更新技能槽状态显示
        /// </summary>
        /// <param name="slotIndex">技能槽索引</param>
        /// <param name="energy">当前能量 (0-100)</param>
        /// <param name="usedInRoom">是否已在本房间使用</param>
        public void UpdateSkillSlotState(int slotIndex, float energy, bool usedInRoom)
        {
            if (_uiView == null) return;

            // 更新能量条显示
            _uiView.SetSkillSlotEnergy(slotIndex, energy / 100f);
            
            // 更新使用状态标记
            _uiView.SetSkillSlotUsed(slotIndex, usedInRoom);
            
            Debug.Log($"[SkillUISystem] 更新技能槽 {slotIndex} 状态: 能量={energy}%, 已使用={usedInRoom}");
        }

        /// <summary>
        /// 刷新所有技能槽显示
        /// </summary>
        /// <param name="playerState">玩家状态</param>
        public void RefreshAllSkillSlots(PlayerRuntimeState playerState)
        {
            if (playerState == null || _uiView == null) return;

            for (int i = 0; i < playerState.SkillSlots.Length; i++)
            {
                var slot = playerState.SkillSlots[i];
                UpdateSkillSlotIcon(i, slot.EquippedSkillId);
                UpdateSkillSlotState(i, slot.Energy, slot.UsedInRoom);
            }
        }

        /// <summary>
        /// 显示技能提示信息
        /// </summary>
        /// <param name="slotIndex">技能槽索引</param>
        /// <param name="skillId">技能ID</param>
        public void ShowSkillTooltip(int slotIndex, string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;

            // 这里可以显示技能详细信息
            // 暂时使用Debug日志
            Debug.Log($"[SkillUISystem] 显示技能提示: 槽位 {slotIndex}, 技能 {skillId}");
        }

        /// <summary>
        /// 隐藏技能提示信息
        /// </summary>
        public void HideSkillTooltip()
        {
            Debug.Log("[SkillUISystem] 隐藏技能提示");
        }
    }
}