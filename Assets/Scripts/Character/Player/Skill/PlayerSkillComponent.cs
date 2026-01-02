using Character.Components.Interface;
using Character.Player.Skill.Core;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Slots;
using UnityEngine;

namespace Character.Player
{
    /// <summary>
    /// 玩家技能组件：重写版，职责最小化
    ///
    /// 架构说明：
    /// - 本组件只作为 MonoBehaviour 入口，协调各服务
    /// - 具体逻辑委托给 SkillExecutor 和 SkillSlotCollection
    /// - 技能执行使用 Phase Pipeline 模式
    /// </summary>
    public sealed class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        // ============ 私有字段（封装） ============
        private SkillSlotCollection _slots;
        private SkillExecutor _executor;

        // ============ 对外 API - 明确语义，不暴露内部集合 ============
        /// <summary>
        /// 获取槽位数量
        /// </summary>
        public int SlotCount => _slots?.Count ?? 0;

        // ============ Unity 生命周期 ============
        private void Awake()
        {
            var inventory = GameRoot.Instance.InventoryManager;
            if (inventory == null)
            {
                Debug.LogError("[PlayerSkillComponent] InventoryServiceManager.Instance is null");
                return;
            }

            //TODO-修改slot大小
            _slots = new SkillSlotCollection(3, inventory);
            _executor = new SkillExecutor(inventory, new EffectFactory());

        }

        private void OnDestroy()
        {
            _executor?.Dispose();
        }

        // ============ ISkillComponent 实现 ============
        /// <summary>
        /// 使用技能
        /// </summary>
        public void UseSkill(int slotIndex, Vector3? aimPoint = null)
        {
            if (!_slots.IsValidIndex(slotIndex)) return;

            var slot = _slots[slotIndex];
            if (slot == null || slot.IsEmpty) return;

            var caster = GetComponent<CharacterBase>();
            if (caster == null) return;

            var aim = aimPoint ?? transform.position;
            _executor.ExecuteSkill(slot, caster, aim);
        }

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        public bool CanUseSkill(int slotIndex)
        {
            if (!_slots.IsValidIndex(slotIndex)) return false;
            var slot = _slots[slotIndex];
            return slot != null && _executor.CanExecute(slot);
        }

        // ============ 公共 API ============
        /// <summary>
        /// 打断技能
        /// </summary>
        /// <param name="slotIndex">槽位索引，-1 表示打断所有技能</param>
        /// <param name="refundCharges">是否退还能量</param>
        public void InterruptSkill(int slotIndex = -1, bool refundCharges = false)
        {
            _executor.Interrupt(slotIndex, refundCharges);
        }

        /// <summary>
        /// 装备主动卡到指定槽位
        /// </summary>
        public void EquipActiveCardToSlotIndex(int slotIndex, string cardId)
        {
            // 获取 PlayerId 以便发布装备事件
            string playerId = null;
            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                var playerState = PlayerManager.Instance?.GetPlayerRuntimeStateByController(controller);
                playerId = playerState?.PlayerId;
            }

            _slots.Equip(slotIndex, cardId, playerId);
        }

        /// <summary>
        /// 卸载指定槽位的主动卡
        /// </summary>
        public void UnequipActiveCardBySlotIndex(int slotIndex)
        {
            var rt = GetRuntime(slotIndex);
            string cardId = rt?.CardId;
            _slots.Unequip(slotIndex);
        }

        /// <summary>
        /// 清空所有槽位
        /// </summary>
        public void ClearAllSlots()
        {
            _slots.ClearAll();
        }

        /// <summary>
        /// 获取槽位
        /// </summary>
        public SkillSlot GetSlot(int slotIndex)
        {
            return _slots[slotIndex];
        }

        /// <summary>
        /// 获取运行时状态
        /// </summary>
        public ActiveSkillRuntime GetRuntime(int slotIndex)
        {
            return _slots[slotIndex]?.Runtime;
        }
    }
}
