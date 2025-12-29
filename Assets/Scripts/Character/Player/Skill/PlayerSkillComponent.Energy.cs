using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using Character.Player.Skill.Runtime;
using RogueGame.Events;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 能量验证部分
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        #region 常量

        private const int DefaultMaxEnergy = 100;

        #endregion

        #region 能量验证

        /// <summary>
        /// 尝试获取指定槽位的运行时数据
        /// </summary>
        private bool TryGetRuntime(int slotIndex, out ActiveSkillRuntime runtime)
        {
            runtime = null;
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;

            var slot = _playerSkillSlots[slotIndex];
            runtime = slot?.Runtime;

            return runtime != null && !string.IsNullOrEmpty(runtime.InstanceId);
        }

        /// <summary>
        /// 检查指定槽位的技能是否有足够能量
        /// </summary>
        public bool HasEnoughEnergy(int slotIndex, int threshold)
        {
            if (!TryGetRuntime(slotIndex, out var rt)) return false;
            if (_inventory == null) return false;

            var state = _inventory.GetActiveCardState(rt.InstanceId);
            return state?.CurrentCharges >= threshold;
        }

        /// <summary>
        /// 获取指定槽位的当前能量
        /// </summary>
        public int GetCurrentEnergy(int slotIndex)
        {
            if (!TryGetRuntime(slotIndex, out var rt)) return 0;
            if (_inventory == null) return 0;

            return _inventory.GetActiveCardState(rt.InstanceId)?.CurrentCharges ?? 0;
        }

        /// <summary>
        /// 获取指定槽位的最大能量
        /// </summary>
        public int GetMaxEnergy(int slotIndex)
        {
            if (!TryGetRuntime(slotIndex, out var rt)) return DefaultMaxEnergy;

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
