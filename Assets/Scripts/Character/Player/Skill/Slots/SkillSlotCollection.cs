namespace Character.Player.Skill.Slots
{
    using Card;
    using Character.Player.Skill.Runtime;
    using Game;
    using System;

    /// <summary>
    /// 槽位集合：管理多个槽位
    /// </summary>
    public sealed class SkillSlotCollection
    {
        private readonly SkillSlot[] _slots;
        private readonly InventoryServiceManager _inventory;

        public SkillSlotCollection(int capacity, InventoryServiceManager inventory)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));

            _slots = new SkillSlot[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _slots[i] = new SkillSlot(i);
            }
            _inventory = inventory;
        }

        /// <summary>
        /// 获取指定索引的槽位
        /// </summary>
        public SkillSlot this[int index]
        {
            get
            {
                if (index < 0 || index >= _slots.Length) return null;
                return _slots[index];
            }
        }

        /// <summary>
        /// 检查索引是否有效
        /// </summary>
        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < _slots.Length;
        }

        /// <summary>
        /// 装备主动卡到指定槽位
        /// </summary>
        public void Equip(int index, string cardId)
        {
            if (!IsValidIndex(index)) return;
            if (string.IsNullOrEmpty(cardId)) return;

            // 从卡牌数据库获取卡牌定义
            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(cardId);
            if (cardDef == null) return;

            // 注意：
            // 这里有意仅获取玩家背包中「该卡牌的首个实例」，
            // 用于将技能运行时与库存中的某个具体实例（InstanceId）建立关联。
            var cardState = _inventory.GetFirstInstanceByCardId(cardId);
            var runtime = new ActiveSkillRuntime(
                cardId,
                cardDef.activeCardConfig?.skill,
                cardState?.InstanceId
            );

            _slots[index].Equip(cardId, runtime);
        }

        /// <summary>
        /// 卸载指定槽位的主动卡
        /// </summary>
        public void Unequip(int index)
        {
            if (!IsValidIndex(index)) return;
            _slots[index].Unequip();
        }

        /// <summary>
        /// 清空所有槽位
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Unequip();
            }
        }

        /// <summary>
        /// 获取槽位数量
        /// </summary>
        public int Count => _slots.Length;
    }
}
