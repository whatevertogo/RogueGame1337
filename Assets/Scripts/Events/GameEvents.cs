using UnityEngine;
using RogueGame.Map;

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

        public RewardSelectionRequestedEvent(int roomId, int instanceId, RoomType roomType, int currentLayer)
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
        public string PlayerId;   // 所属玩家ID
        public int NewEnergy;
        public int MaxEnergy;     // 最大能量（UI层可直接计算归一化值）
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
        public int MaxEnergy;     // 预先携带最大能量，避免后续查询
    }

    /// <summary>
    /// 技能进化请求事件：当重复获得主动卡时发布，触发进化选择UI
    /// </summary>
    public class SkillEvolutionRequestedEvent
    {
        public string CardId;
        public string InstanceId;
        public int CurrentLevel;
        public int NextLevel;
        public Character.Player.Skill.Evolution.SkillNode EvolutionNode;

        public SkillEvolutionRequestedEvent(string cardId, string instanceId, int currentLevel, int nextLevel, Character.Player.Skill.Evolution.SkillNode evolutionNode)
        {
            CardId = cardId;
            InstanceId = instanceId;
            CurrentLevel = currentLevel;
            NextLevel = nextLevel;
            EvolutionNode = evolutionNode;
        }
    }

    /// <summary>
    /// 技能进化完成事件：分支选择后发布，确认进化生效
    /// </summary>
    public class SkillEvolvedEvent
    {
        public string CardId;
        public string InstanceId;
        public int NewLevel;
        public Character.Player.Skill.Evolution.SkillBranch SelectedBranch;
        public string BranchPath; // 例如 "A-A-B-A"

        public SkillEvolvedEvent(string cardId, string instanceId, int newLevel, Character.Player.Skill.Evolution.SkillBranch selectedBranch, string branchPath)
        {
            CardId = cardId;
            InstanceId = instanceId;
            NewLevel = newLevel;
            SelectedBranch = selectedBranch;
            BranchPath = branchPath;
        }
    }


}
