using System;
using Character.Player;
using RogueGame.Events;
using RogueGame.Map;
using UnityEngine;

namespace RogueGame.Game.Service.SkillLimit
{
    /// <summary>
    /// 房间技能限制服务：规则驱动的协调器
    /// 职责：监听房间事件，根据房间规则对玩家技能系统下达限制/重置指令
    ///
    /// 设计原则：
    /// - 只负责一件事：根据"房间规则"限制或重置玩家技能的可用状态
    /// - 不负责技能释放、不计算伤害、不管 UI、不持有复杂状态
    /// - 监听房间事件（进入/退出/清理），对 PlayerSkillLimiter 下达指令
    /// - 面向接口/意图，不直接操作 Player 内部细节
    /// </summary>
    public sealed class RoomPlayerSkillLimitService
    {
        private readonly RoomManager roomManager;
        private readonly PlayerManager _playerManager;
        private bool _subscribed = false;

        // 当前房间规则（用于调试）
        private RoomSkillRule _currentRule = RoomSkillRule.None;

        public RoomPlayerSkillLimitService(PlayerManager playerManager, RoomManager roomManager)
        {
            this.roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe()
        {
            if (_subscribed) return;

            EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Subscribe<RoomClearedEvent>(OnRoomCleared);
            _subscribed = true;

            CDTU.Utils.CDLogger.Log("[RoomPlayerSkillLimitService] 已订阅房间事件");
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe()
        {
            if (!_subscribed) return;

            EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Unsubscribe<RoomClearedEvent>(OnRoomCleared);
            _subscribed = false;
        }

        /// <summary>
        /// 房间进入事件处理
        /// </summary>
        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (evt == null) return;

            // 获取本地玩家
            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                CDTU.Utils.CDLogger.LogWarning("[RoomPlayerSkillLimitService] 没有本地玩家");
                return;
            }

            // 获取房间控制器
            var roomController = GetRoomController(evt.RoomId, evt.InstanceId);
            if (roomController == null)
            {
                CDTU.Utils.CDLogger.LogWarning($"[RoomPlayerSkillLimitService] 找不到房间控制器 (RoomId={evt.RoomId}, InstanceId={evt.InstanceId})");
                return;
            }

            // 应用房间技能规则
            ApplySkillRule(roomController, localPlayer);
        }

        /// <summary>
        /// 房间清理完成事件处理（战斗结束，可以切换技能）
        /// </summary>
        private void OnRoomCleared(RoomClearedEvent evt)
        {
            if (evt == null) return;

            // 房间清理完成后，技能限制仍然生效，但玩家可以在背包中切换技能
            // 这里不做任何操作，只是记录日志
            CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间清理完成 (RoomId={evt.RoomId})");
        }

        /// <summary>
        /// 应用房间技能规则到玩家
        /// </summary>
        private void ApplySkillRule(RoomController room, PlayerController player)
        {
            if (room == null || player == null) return;

            // 获取房间的技能规则
            RoomSkillRule rule = GetRoomSkillRule(room);
            _currentRule = rule;

            // 获取玩家的技能限制器
            var skillLimiter = player.GetComponent<PlayerSkillComponent>()?.SkillLimiter;
            if (skillLimiter == null)
            {
                CDTU.Utils.CDLogger.LogError("[RoomPlayerSkillLimitService] 玩家没有 PlayerSkillLimiter");
                return;
            }

            // 清除之前的规则
            skillLimiter.Clear();

            // 重置房间使用记录
            skillLimiter.ResetRoomUsage();

            // 根据规则应用限制
            switch (rule)
            {
                case RoomSkillRule.DisableAllSkills:
                    skillLimiter.DisableAll();
                    CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间 {room.RoomMeta.Index}: 禁用所有技能");
                    break;

                case RoomSkillRule.OneTimePerRoom:
                    skillLimiter.SetOneTimeLimit();
                    CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间 {room.RoomMeta.Index}: 每个房间只能使用一次技能");
                    break;

                case RoomSkillRule.ResetOnEnter:
                    ResetSkillEnergy(player);
                    CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间 {room.RoomMeta.Index}: 重置技能充能");
                    break;

                case RoomSkillRule.NoCooldown:
                    skillLimiter.SetNoCooldownMode();
                    CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间 {room.RoomMeta.Index}: 无冷却模式（测试用）");
                    break;

                case RoomSkillRule.None:
                default:
                    CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间 {room.RoomMeta.Index}: 无技能限制");
                    break;
            }
        }

        /// <summary>
        /// 获取房间的技能规则
        /// </summary>
        private RoomSkillRule GetRoomSkillRule(RoomController room)
        {
            // 检查 RoomMeta 是否实现了 ISkillRuleProvider
            if (room.RoomMeta is ISkillRuleProvider provider)
            {
                return provider.SkillRule;
            }

            // 默认规则：每个房间只能使用一次技能
            return RoomSkillRule.OneTimePerRoom;
        }

        /// <summary>
        /// 重置技能充能
        /// </summary>
        private void ResetSkillEnergy(PlayerController player)
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null) return;

            var skillComponent = player.GetComponent<PlayerSkillComponent>();
            if (skillComponent == null) return;

            // 重置所有技能槽的充能
            foreach (var slot in skillComponent.PlayerSkillSlots)
            {
                if (slot?.Runtime != null && !string.IsNullOrEmpty(slot.Runtime.InstanceId))
                {
                    var state = inventory.GetActiveCard(slot.Runtime.InstanceId);
                    if (state != null)
                    {
                        var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(slot.Runtime.CardId);
                        if (cardDef?.activeCardConfig != null)
                        {
                            // 重置为最大能量
                            int maxEnergy = cardDef.activeCardConfig.maxEnergy;
                            inventory.SetCharges(slot.Runtime.InstanceId, maxEnergy);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取本地玩家
        /// </summary>
        private PlayerController GetLocalPlayer()
        {
            var playerData = _playerManager.GetLocalPlayerData();
            return playerData?.Controller;
        }

        /// <summary>
        /// 获取房间控制器
        /// </summary>
        private RoomController GetRoomController(int roomId, int instanceId)
        {
            if (roomManager == null) return null;

            return roomManager.GetCurrentRoomController();
        }

        /// <summary>
        /// 获取当前应用的规则（用于调试）
        /// </summary>
        public RoomSkillRule GetCurrentRule() => _currentRule;
    }
}
