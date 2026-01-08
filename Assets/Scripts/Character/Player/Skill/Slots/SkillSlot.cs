namespace Character.Player.Skill.Slots
{
    using Character.Player.Skill.Runtime;

    /// <summary>
    /// 技能槽位：数据结构
    /// </summary>
    public sealed class SkillSlot
    {
        /// <summary>
        /// 槽位索引
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 技能运行时状态
        /// </summary>
        public ActiveSkillRuntime Runtime { get; private set; }

        /// <summary>
        /// 槽位是否为空
        /// </summary>
        public bool IsEmpty => Runtime == null;

        public SkillSlot(int index)
        {
            Index = index;
        }

        /// <summary>
        /// 装备技能到槽位
        /// </summary>
        public void Equip(string cardId, ActiveSkillRuntime runtime)
        {
            Runtime = runtime;
        }

        /// <summary>
        /// 卸载槽位技能
        /// </summary>
        public void Unequip()
        {
            Runtime = null;
        }
    }
}
