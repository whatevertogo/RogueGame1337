namespace Character
{
    /// <summary>
    /// 效果实例感知伤害来源接口
    /// 替代反射调用，提升性能
    /// </summary>
    public interface IDamageSourceAware
    {
        /// <summary>
        /// 设置伤害来源（施法者）
        /// </summary>
        void SetDamageSource(CharacterBase source);
    }
}
