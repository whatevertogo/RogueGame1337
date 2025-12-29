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
        /// 缓存的服务引用（避免重复获取单例）
        /// </summary>
        private InventoryServiceManager _inventory;

        /// <summary>
        /// 无冷却模式开关。
        ///
        /// 使用说明：
        /// - 默认值：false（关闭），即按照技能自身冷却时间正常结算。
        /// - 典型用途：用于开发期 / QA 调试（例如快速验证技能数值、连招手感等），不建议在正式玩法中长期开启。
        /// - 推荐控制方式：通过外部调试面板、配置服务或关卡脚本，在合适的时机调用 <see cref="SetNoCooldownMode(bool)"/> 进行显式开启 / 关闭。
        ///
        /// 生命周期与管理：
        /// - 本字段为组件实例级状态，随 <see cref="PlayerSkillComponent"/> 的创建而初始化，随组件销毁（如场景卸载）自动失效，无需手动重置。
        /// - 建议在：
        ///     * 进入战斗 / 调试场景后，由外部服务读取配置并调用 <see cref="SetNoCooldownMode(bool)"/> 进行初始化；
        ///     * 退出战斗 / 关闭调试功能前，显式调用 <see cref="SetNoCooldownMode(false)"/> 以恢复正常冷却逻辑。
        /// - 如需根据不同关卡 / 账号配置动态切换，可在对应事件（如关卡加载完成、玩家设置变更）中调用 <see cref="SetNoCooldownMode(bool)"/>。
        /// </summary>
        private bool _noCooldownMode = false;

        /// <summary>
        /// 设置无冷却模式开关。
        ///
        /// 推荐由外部系统调用的典型场景：
        /// - 调试 / GM 面板：提供一个“无冷却模式”复选框，勾选时调用 <c>SetNoCooldownMode(true)</c>，取消勾选调用 <c>SetNoCooldownMode(false)</c>。
        /// - 配置 / 开关服务：根据账号、角色或关卡配置，在进入战斗时自动开启 / 关闭无冷却模式。
        /// - 自动化测试：在测试用例初始化阶段统一开启无冷却模式，以缩短测试时间。
        ///
        /// 注意：
        /// - 此方法只修改当前玩家技能组件实例上的状态，不会影响其他玩家或全局逻辑。
        /// - 建议仅在开发环境、测试环境或受控的调试场景中使用；正式线上环境应谨慎开启。
        /// </summary>
        /// <param name="enabled">true 表示启用无冷却模式，false 表示恢复正常冷却。</param>
        public void SetNoCooldownMode(bool enabled)
        {
            _noCooldownMode = enabled;
        }
        #endregion

        #region 生命周期

        private void Awake()
        {
            _inventory = InventoryServiceManager.Instance;
            if (_inventory == null)
                CDTU.Utils.CDLogger.LogError("[PlayerSkillComponent] InventoryServiceManager.Instance is null");

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
