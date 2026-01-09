using System.Collections.Generic;
using RogueGame.Map;
using UnityEngine;

namespace RogueGame.Events
{
    /// <summary>
    /// 房间进入事件：UI 层可订阅以显示房间进入提示
    /// </summary>
    public class RoomEnteredEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
    }

    /// <summary>
    /// 战斗开始事件：UI 层可订阅以显示战斗开始提示
    /// </summary>
    public class CombatStartedEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
    }

    /// <summary>
    /// 房间进入事件：UI 层可订阅以显示房间进入提示
    /// </summary>
    public class RoomClearedEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
        public int ClearedEnemyCount;
    }

    /// <summary>
    /// 房间清除事件：UI 层可订阅以显示房间清除提示
    /// </summary>
    public class ChooseNextRoomEvent
    {
        public int FromRoomId;
        public int FromRoomInstanceId;
    }

    /// <summary>
    /// 层过渡事件：UI 层可订阅以处理层过渡逻辑
    /// </summary>
    public class LayerTransitionEvent
    {
        public int FromLayer;
        public int ToLayer;

        public LayerTransitionEvent(int fromLayer, int toLayer)
        {
            FromLayer = fromLayer;
            ToLayer = toLayer;
        }
    }

    /// <summary>
    /// 门进入请求事件：UI 层可订阅以处理门进入逻辑
    /// </summary>
    public class DoorEnterRequestedEvent
    {
        public Direction Direction;
        public int RoomId;
        public int InstanceId;
    }

    /// <summary>
    /// 开始运行事件：UI 层可订阅以显示开始运行提示
    /// </summary>
    public class StartRunRequestedEvent
    {
        public RogueGame.Map.RoomMeta StartMeta;
        public int InitialRoomId;
    }

    // 由 GameFlowCoordinator 发布奖励选择（从事实事件触发）
    // FloorRewardSystemService来实行
    public class RewardSelectionRequestedEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
        public int CurrentLayer;

        public RewardSelectionRequestedEvent(
            int roomId,
            int instanceId,
            RoomType roomType,
            int currentLayer
        )
        {
            RoomId = roomId;
            InstanceId = instanceId;
            RoomType = roomType;
            CurrentLayer = currentLayer;
        }
    }

    /// <summary>
    /// 奖励授予事件：UI 层可订阅以显示奖励获取提示
    /// </summary>
    public class RewardGrantedEvent
    {
        public int RoomId;
        public int InstanceId;
        public string RewardId; // 简化的奖励标识
    }

    /// <summary>
    /// 交互提示事件：UI 层可订阅以显示/隐藏交互提示（例如按 E 进入）
    /// </summary>
    public class InteractionPromptEvent
    {
        public string Message;
        public bool Show;
    }

    public class CoinTextUpdateEvent
    {
        public string NewText;

        public CoinTextUpdateEvent(string newText)
        {
            NewText = newText;
        }
    }

    /// <summary>
    /// 卡牌获取事件：UI 层可订阅以显示卡牌获取提示
    /// </summary>
    public class CardAcquiredEvent
    {
        public string PlayerId;
        public CardAcquisitionSource Source;
    }

    /// <summary>
    /// Boss 解锁事件：UI 层可订阅以显示 Boss 解锁提示
    /// </summary>
    public class BossUnlockedEvent
    {
        public int Layer;
    }

    /// <summary>
    /// 实体被击杀事件：包含被击杀对象与击杀者（击杀者可能为 null）
    /// 发布者（例如 RoomController / EnemyCharacter）可在死亡回调中发布此事件以便订阅方（例如 RunInventory / PlayerManager）处理能量/充能/掉落等逻辑。
    /// </summary>
    public class EntityKilledEvent
    {
        /// <summary>
        /// 被击杀的实体（GameObject）
        /// </summary>
        public GameObject Victim;

        /// <summary>
        /// 击杀者（可能为 null，例如环境伤害）
        /// </summary>
        public GameObject Attacker;

        /// <summary>
        /// 可选：发生击杀时的房间类型（便于按房间类型调整奖励）
        /// </summary>
        public RoomType RoomType;

        /// <summary>
        /// 可选：如果击杀者是玩家，此处可填 PlayerId（若发送方能解析）
        /// </summary>
        public string AttackerPlayerId;
    }

    /// <summary>
    /// 玩家技能能量变化事件：PlayerSkillComponent 在技能能量变化时发布此事件
    /// </summary>
    public class OnPlayerSkillEquippedEvent
    {
        public string PlayerId;
        public int SlotIndex;
        public string NewCardId; // 可能为 null，表示槽位被清空

        public OnPlayerSkillEquippedEvent(string playerId, int slotIndex, string newCardId)
        {
            PlayerId = playerId;
            SlotIndex = slotIndex;
            NewCardId = newCardId;
        }
    }

    /// <summary>
    /// 请求清空所有槽位（由 UI 或逻辑发布，请专门的系统订阅并执行具体清理）
    /// </summary>
    public class ClearAllSlotsRequestedEvent
    {
        public string PlayerId; // 可选：限定针对某玩家（若为空则对所有玩家生效）

        public ClearAllSlotsRequestedEvent(string playerId = null)
        {
            PlayerId = playerId;
        }
    }

    /// <summary>
    /// 玩家死亡事件：PlayerController 在玩家死亡时发布此事件
    /// </summary>
    public class PlayerDiedEvent
    {
        public PlayerController Player;

        public PlayerDiedEvent(PlayerController player)
        {
            Player = player;
        }
    }

    /// <summary>
    /// 被动卡牌拾取事件：InventoryManager 在添加被动卡时发布此事件
    /// </summary>
    public class PassiveCardAcquiredEvent
    {
        public string CardId;
        public int Count;
        public CardAcquisitionSource Source;

        public PassiveCardAcquiredEvent(string cardId, int count, CardAcquisitionSource source)
        {
            CardId = cardId;
            Count = count;
            Source = source;
        }
    }

    /// <summary>
    /// 被动卡牌移除事件：InventoryManager 在移除被动卡时发布此事件
    /// </summary>
    public class PassiveCardRemovedEvent
    {
        public string CardId;
        public int Count;

        public PassiveCardRemovedEvent(string cardId, int count)
        {
            CardId = cardId;
            Count = count;
        }
    }

    /// <summary>
    /// 玩家重生事件：玩家重生后发布，用于重新应用被动效果
    /// </summary>
    public class PlayerRespawnEvent
    {
        public string PlayerId;

        public PlayerRespawnEvent(string playerId)
        {
            PlayerId = playerId;
        }
    }

    /// <summary>
    /// 玩家技能释放事件：PlayerSkillComponent 在释放技能时发布此事件
    /// 用于被动卡牌（例如暴风骤雨）监听技能释放并触发效果
    /// </summary>
    public class PlayerSkillCastEvent
    {
        public string PlayerId;
        public int SlotIndex; // 0=Q技能, 1=E技能
        public string CardId; // 释放的技能卡牌ID

        public PlayerSkillCastEvent(string playerId, int slotIndex, string cardId)
        {
            PlayerId = playerId;
            SlotIndex = slotIndex;
            CardId = cardId;
        }
    }

    /// <summary>
    /// 主动卡能量变化事件：ActiveCardService 在能量增减时发布此事件
    /// UI 层订阅此事件以自动更新能量条
    /// </summary>
    public class ActiveCardEnergyChangedEvent
    {
        public string InstanceId;
        public string PlayerId; // 所属玩家ID
        public int NewEnergy;
        public int MaxEnergy; // 最大能量（UI层可直接计算归一化值）
    }

    /// <summary>
    /// 技能槽装备事件：卡牌装备到槽位时发布此事件
    /// UI 层订阅此事件以建立 InstanceId → SlotIndex 的映射
    /// </summary>
    public class SkillSlotEquippedEvent
    {
        public string PlayerId;
        public int SlotIndex;
        public string InstanceId;
        public int MaxEnergy; // 预先携带最大能量，避免后续查询
    }

    /// <summary>
    /// 技能进化请求事件：当重复获得主动卡时发布，触发进化选择UI
    /// 效果池系统：携带从效果池获取的随机选项列表
    /// </summary>
    public class SkillEvolutionRequestedEvent
    {
        public string CardId;
        public string InstanceId;
        public int CurrentLevel;
        public int NextLevel;

        /// <summary>
        /// 可选的进化效果列表（从效果池随机生成）
        /// </summary>
        public List<Character.Player.Skill.Evolution.EvolutionEffectEntry> Options;

        public SkillEvolutionRequestedEvent(
            string cardId,
            string instanceId,
            int currentLevel,
            int nextLevel,
            List<Character.Player.Skill.Evolution.EvolutionEffectEntry> options
        )
        {
            CardId = cardId;
            InstanceId = instanceId;
            CurrentLevel = currentLevel;
            NextLevel = nextLevel;
            Options = options;
        }
    }

    /// <summary>
    /// 技能进化完成事件：玩家选择进化效果后发布，确认进化生效
    /// 效果池系统：携带选中的进化效果
    /// </summary>
    public class SkillEvolvedEvent
    {
        public string CardId;
        public string InstanceId;
        public int NewLevel;

        /// <summary>
        /// 选中的进化效果（效果池系统）
        /// </summary>
        public Character.Player.Skill.Evolution.EvolutionEffectEntry SelectedEffect;

        public SkillEvolvedEvent(
            string cardId,
            string instanceId,
            int newLevel,
            Character.Player.Skill.Evolution.EvolutionEffectEntry selectedEffect
        )
        {
            CardId = cardId;
            InstanceId = instanceId;
            NewLevel = newLevel;
            SelectedEffect = selectedEffect;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 连击系统事件
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 连击变化事件：连击计数或连击等级变化时发布
    /// UI 层订阅此事件以更新连击显示
    /// </summary>
    public class ComboChangedEvent
    {
        /// <summary>
        /// 当前连击数
        /// </summary>
        public int CurrentCombo;

        /// <summary>
        /// 连击等级（0=普通, 1=狂热, 2=屠戮, 3=毁灭）
        /// </summary>
        public int ComboTier;

        /// <summary>
        /// 连击等级名称
        /// </summary>
        public string TierName;

        /// <summary>
        /// 能量获取加成比例（例如 1.5 表示 +50%）
        /// </summary>
        public float EnergyBonusMultiplier;

        /// <summary>
        /// 连击是否即将超时（剩余时间 < 1秒）
        /// </summary>
        public bool IsAboutToExpire;

        public ComboChangedEvent(
            int currentCombo,
            int comboTier,
            string tierName,
            float energyBonusMultiplier,
            bool isAboutToExpire
        )
        {
            CurrentCombo = currentCombo;
            ComboTier = comboTier;
            TierName = tierName;
            EnergyBonusMultiplier = energyBonusMultiplier;
            IsAboutToExpire = isAboutToExpire;
        }
    }

    /// <summary>
    /// 连击中断事件：连击超时重置时发布
    /// UI 层订阅此事件以显示中断效果
    /// </summary>
    public class ComboExpiredEvent
    {
        /// <summary>
        /// 中断前的连击数
        /// </summary>
        public int FinalCombo;

        /// <summary>
        /// 中断前的连击等级
        /// </summary>
        public int FinalTier;

        public ComboExpiredEvent(int finalCombo, int finalTier)
        {
            FinalCombo = finalCombo;
            FinalTier = finalTier;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 打击感反馈事件
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 屏幕震动请求事件：需要屏幕震动时发布
    /// </summary>
    public class ScreenShakeRequestedEvent
    {
        /// <summary>
        /// 震动强度（0-1，建议值：微幅0.05，中幅0.15，大幅0.3）
        /// </summary>
        public float Intensity;

        /// <summary>
        /// 震动持续时间（秒）
        /// </summary>
        public float Duration;

        /// <summary>
        /// 震动类型
        /// </summary>
        public ShakeType ShakeType;

        public ScreenShakeRequestedEvent(
            float intensity,
            float duration,
            ShakeType shakeType = ShakeType.Default
        )
        {
            Intensity = intensity;
            Duration = duration;
            ShakeType = shakeType;
        }
    }

    /// <summary>
    /// 震动类型
    /// </summary>
    public enum ShakeType
    {
        /// <summary>默认震动</summary>
        Default,

        /// <summary>水平震动</summary>
        Horizontal,

        /// <summary>垂直震动</summary>
        Vertical,

        /// <summary>旋转震动</summary>
        Rotation,
    }

    /// <summary>
    /// 帧停止请求事件：需要时间冻结时发布
    /// </summary>
    public class HitStopRequestedEvent
    {
        /// <summary>
        /// 停止持续时间（秒，建议值：0.03-0.1）
        /// </summary>
        public float Duration;

        /// <summary>
        /// 是否逐渐恢复（true 则平滑过渡，false 则立即恢复）
        /// </summary>
        public bool GradualRecovery;

        public HitStopRequestedEvent(float duration, bool gradualRecovery = true)
        {
            Duration = duration;
            GradualRecovery = gradualRecovery;
        }
    }

    /// <summary>
    /// 敌人击退请求事件：需要击退敌人时发布
    /// </summary>
    public class EnemyKnockbackRequestedEvent
    {
        /// <summary>
        /// 被击退的敌人
        /// </summary>
        public GameObject Enemy;

        /// <summary>
        /// 击退方向（从攻击者指向敌人）
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// 击退力度
        /// </summary>
        public float Force;

        /// <summary>
        /// 击退持续时间（秒）
        /// </summary>
        public float Duration;

        public EnemyKnockbackRequestedEvent(
            GameObject enemy,
            Vector3 direction,
            float force,
            float duration = 0.2f
        )
        {
            Enemy = enemy;
            Direction = direction;
            Force = force;
            Duration = duration;
        }
    }
}
