using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using RogueGame.Events;

namespace Character.Player
{
    /// <summary>
    /// 玩家技能组件：管理技能槽位、冷却与触发
    ///
    /// 重构说明：
    /// - 本文件为主组件文件，包含字段声明和生命周期方法
    /// - 具体实现按职责拆分到以下 partial class 文件：
    ///   - PlayerSkillComponent.Slots.cs    - 槽位管理
    ///   - PlayerSkillComponent.Casting.cs  - 技能释放
    ///   - PlayerSkillComponent.Energy.cs   - 能量验证
    ///   - PlayerSkillComponent.Interrupt.cs - 打断控制
    ///   - PlayerSkillComponent.Events.cs   - 事件处理
    ///
    /// 修改器系统支持：
    /// - ActiveSkillRuntime 支持修改器添加/移除/应用
    /// - 修改器通过 ISkillModifier 接口定义
    /// - 技能释放时自动应用所有活动修改器
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        #region 字段

        [SerializeField, ReadOnly]
        private SkillSlot[] _playerSkillSlots = new SkillSlot[2];

        /// <summary>
        /// 技能槽位数组（对外只读）
        /// </summary>
        public SkillSlot[] PlayerSkillSlots => _playerSkillSlots;

        /// <summary>
        /// 技能限制器：根据房间规则限制技能使用
        /// </summary>
        public PlayerSkillLimiter SkillLimiter;


        /// <summary>
        /// 缓存的服务引用（避免重复获取单例）
        /// </summary>
        private InventoryServiceManager _inventory;


        #endregion

        #region 生命周期

        private void Awake()
        {
            _inventory = InventoryServiceManager.Instance;
            // TODO-将其加入难度系统里面通过服务模式
            SkillLimiter = new PlayerSkillLimiter();
            SkillLimiter.SetNoCooldownMode(); // 默认无冷却模式

            // 确保数组内的 SkillSlot 实例已初始化，避免 Inspector VS 运行期不一致
            if (_playerSkillSlots != null)
            {
                for (int i = 0; i < _playerSkillSlots.Length; i++)
                {
                    if (_playerSkillSlots[i] == null)
                        _playerSkillSlots[i] = new SkillSlot();
                }
            }

            // 订阅事件
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            // 取消订阅事件
            UnsubscribeEvents();
        }

        #endregion
    }
}
