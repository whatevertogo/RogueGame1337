using System;
using Character.Player;
using Core.Events;
using RogueGame.Events;
using RogueGame.Map;
using UnityEngine;

namespace RogueGame.Game.Service.SkillLimit
{
    /// <summary>
    /// 房间技能限制服务：监听房间事件，根据房间技能规则调整玩家技能状态
    /// 当前实现：当进入房间时重置玩家技能能量；DisableAllSkills 等规则仅保留处理框架，尚未启用
    /// 注：系统已改为纯充能模式，不再有技能冷却
    /// </summary>
    public sealed class RoomPlayerSkillLimitService
    {
        private readonly RoomManager roomManager;
        private readonly PlayerManager _playerManager;
        private bool _subscribed = false;

        // 当前房间规则（用于调试）
        private RoomSkillRule _currentRule = RoomSkillRule.None;
        private readonly InventoryServiceManager inventory;

        public RoomPlayerSkillLimitService(PlayerManager playerManager, RoomManager roomManager, InventoryServiceManager inventoryManager)
        {
            this.roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            this.inventory = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe()
        {
            if (_subscribed) return;

            EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
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
            _subscribed = false;
        }

        /// <summary>
        /// 房间进入事件处理
        /// </summary>

        /// <param name="evt">房间进入事件对象，包含房间相关信息</param>
        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            // 检查事件对象是否为空
            if (evt == null) return;

            // 获取本地玩家对象
            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                // 如果没有找到本地玩家，记录警告日志并退出
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
        /// 应用房间技能规则到玩家
        /// </summary>
        private void ApplySkillRule(RoomController room, PlayerController player)
        {
            if (room == null || player == null) return;

            // 获取房间的技能规则
            RoomSkillRule rule = GetRoomSkillRule(room);
            _currentRule = rule;

            switch (rule)
            {
                case RoomSkillRule.ResetOnEnter:
                    ResetSkillEnergy(player);
                    CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间 {room.RoomMeta.Index}: 重置技能充能");
                    break;

                case RoomSkillRule.None:
                default:
                    CDTU.Utils.CDLogger.Log($"[RoomPlayerSkillLimitService] 房间 {room.RoomMeta.Index}: 无特殊规则");
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

            // 默认规则：无技能使用限制
            return RoomSkillRule.None;
        }

        /// <summary>
        /// 重置技能充能
        /// </summary>
        private void ResetSkillEnergy(PlayerController player)
        {
            if (inventory == null) return;

            var skillComponent = player.GetComponent<PlayerSkillComponent>();
            if (skillComponent == null) return;

            // 重置所有技能槽的充能
            for (int i = 0; i < skillComponent.SlotCount; i++)
            {
                var rt = skillComponent.GetRuntime(i);
                if (rt != null && !string.IsNullOrEmpty(rt.InstanceId))
                {
                    //TODO-以后考虑优雅
                    inventory.ActiveCardService.AddEnergy(rt.InstanceId,10000);
                }
            }
        }

        /// <summary>
        /// 获取本地玩家
        /// </summary>
        private PlayerController GetLocalPlayer()
        {
            var playerData = _playerManager.GetLocalPlayerRuntimeState();
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
