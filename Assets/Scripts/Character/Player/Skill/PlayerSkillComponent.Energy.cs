using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using Character.Player.Skill.Runtime;
using RogueGame.Events;
using CDTU.Utils;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 能量验证部分
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        /// <summary>
        /// 默认最大能量值。
        /// 注意：该值应与卡牌配置（如 ActiveCardConfig / 配置表）中 maxEnergy 的默认值保持一致，
        /// 如需调整默认最大能量，请同时修改配置与此常量，避免出现数据不同步的问题。
        /// </summary>
        private const int DefaultMaxEnergy = 100;

        #region 能量验证

        /// <summary>
        /// 尝试获取指定槽位的运行时数据（调用方需保证 slotIndex 有效）
        /// </summary>
        private bool TryGetRuntime(int slotIndex, out ActiveSkillRuntime runtime)
        {
            // 防御式检查：即使调用方未验证索引，也不应抛出异常
            runtime = null;

            if (_playerSkillSlots == null)
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length)
            {
                return false;
            }
            runtime = _playerSkillSlots[slotIndex]?.Runtime;
            return runtime != null && !string.IsNullOrEmpty(runtime.InstanceId);
        }

        /// <summary>
        /// 检查指定槽位的技能是否有足够能量
        /// </summary>
        public bool HasEnoughEnergy(int slotIndex, int threshold)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;
            if (!TryGetRuntime(slotIndex, out var rt)) return false;

            var state = _inventory.GetActiveCardState(rt.InstanceId);
            return state?.CurrentCharges >= threshold;
        }

        /// <summary>
        /// 获取指定槽位的当前能量
        /// </summary>
        public int GetCurrentEnergy(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return 0;
            if (!TryGetRuntime(slotIndex, out var rt)) return 0;

            return _inventory.GetActiveCardState(rt.InstanceId)?.CurrentCharges ?? 0;
        }

        /// <summary>
        /// 获取指定槽位的最大能量
        /// </summary>
        public int GetMaxEnergy(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return DefaultMaxEnergy;
            if (!TryGetRuntime(slotIndex, out var rt))
            {
                CDLogger.LogWarning($"[PlayerSkillComponent] GetMaxEnergy failed: invalid runtime for slotIndex={slotIndex}");
                return DefaultMaxEnergy;
            }

            var config = rt.CachedActiveConfig;
            return Mathf.Max(1, config?.maxEnergy ?? DefaultMaxEnergy);
        }

        /// <summary>
        /// 获取能量比例（0-1，用于 UI 显示）
        /// </summary>
        public float GetEnergyRatio(int slotIndex)
        {
            int max = GetMaxEnergy(slotIndex);
            if (max <= 0) return 0f;
            return (float)GetCurrentEnergy(slotIndex) / max;
        }

        /// <summary>
        /// 当能量变化时，自动更新对应槽位的 UI
        /// </summary>
        private void OnEnergyChangedFromInventory(ActiveCardChargesChangedEvent evt)
        {
            // 查找使用此 instanceId 的槽位
            for (int i = 0; i < _playerSkillSlots.Length; i++)
            {
                var rt = _playerSkillSlots[i]?.Runtime;
                if (rt != null && rt.InstanceId == evt.InstanceId)
                {
                    // 使用缓存配置获取最大能量并归一化
                    int maxEnergy = rt.CachedActiveConfig?.maxEnergy ?? DefaultMaxEnergy;
                    float norm = maxEnergy > 0 ? (float)evt.NewCharges / maxEnergy : 0f;
                    OnEnergyChanged?.Invoke(i, norm);
                    break;
                }
            }
        }

        #endregion
    }
}
