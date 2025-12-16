using System;
using System.Collections.Generic;
using UnityEngine;
using Character.Components;
using RogueGame.Events;

namespace RogueGame.Map
{
    /// <summary>
    /// 房间控制器 - 管理战斗、敌人、奖励
    /// 房间状态机 + 流程编排者
    /// </summary>
    [RequireComponent(typeof(RoomPrefab))]
    public sealed class RoomController : MonoBehaviour
    {
        [Header("敌人配置"), InlineEditor]
        [SerializeField] private EnemySpawnConfig enemySpawnConfig;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private int minEnemies = 3;
        [SerializeField] private int maxEnemies = 6;

        [Header("精英/Boss")]
        [SerializeField] private Transform eliteAndbossSpawnPoint;

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField][ReadOnly] private RoomState currentState = RoomState.Inactive;
        [SerializeField][ReadOnly] private RoomType roomType = RoomType.Normal;
        [SerializeField][ReadOnly] private int enemyCount = 0;
        [SerializeField][ReadOnly] private int initialEnemyCount = 0;

        private RoomPrefab roomPrefab;
        private RoomMeta roomMeta;

        public RoomMeta RoomMeta => roomMeta;
        private readonly List<GameObject> activeEnemies = new();
        //事件订阅者
        private readonly Dictionary<GameObject, Action> _enemyDeathHandlersDic = new();
        private readonly Dictionary<GameObject, Action<GameObject>> _enemyDeathWithAttackerHandlersDic = new();
        private int currentFloor = 1;
        // 记录玩家进入该房间时使用的入口方向，用于战斗期间与战斗结束时的门状态控制
        private Direction lastEntryDirection = Direction.None;

        // ========== 事件 (已迁移到 EventBus) ==========
        // 房间激活/战斗开始/房间清理已通过全局 EventBus 发布，减少耦合。
        public event Action<RoomController, int> OnEnemyKilled;

        // 转发器: 帮助将 HealthComponent 的事件转发到 RoomController 并携带 enemy 引用
        private class EnemyDeathForwarder
        {
            private readonly RoomController owner;
            private readonly GameObject enemy;

            public EnemyDeathForwarder(RoomController owner, GameObject enemy)
            {
                this.owner = owner;
                this.enemy = enemy;
            }

            public void OnDeath() => owner.OnEnemyDeath(enemy);
            public void OnDeathWithAttacker(GameObject attacker) => owner.OnEnemyDeathWithKiller(enemy, attacker);
        }

        // ========== 属性 ==========
        public RoomState CurrentState => currentState;
        public RoomType RoomType => roomType;
        public RoomPrefab Prefab => roomPrefab;
        public int EnemyCount => activeEnemies.Count;

        public bool IsCleared => currentState == RoomState.Cleared ||
                                 currentState == RoomState.Completed;

        public bool IsCombatRoom => roomType == RoomType.Normal ||
                                    roomType == RoomType.Elite ||
                                    roomType == RoomType.Boss;

        public bool CanPlayerLeave
        {
            get
            {
                if (!IsCombatRoom) return true;
                if (IsCleared) return true;
                if (currentState == RoomState.Combat)
                {
                    return activeEnemies.Count <= 0;
                }
                return true;
            }
        }

        public string GetCannotLeaveReason()
        {
            if (CanPlayerLeave) return null;

            if (currentState == RoomState.Combat && activeEnemies.Count > 0)
            {
                return $"战斗中！还有 {activeEnemies.Count} 个敌人";
            }

            return "无法离开";
        }


        private void Awake()
        {
            roomPrefab = GetComponent<RoomPrefab>();
            // 不再自动查找生成点，改为手动在编辑器或代码中配置
        }

        /// <summary>
        /// 手动设置敌人出生点（由外部或编辑器调用）
        /// </summary>
        public void SetEnemySpawnPoints(Transform[] points)
        {
            enemySpawnPoints = points;
        }

        /// <summary>
        /// 手动设置 Boss 出生点
        /// </summary>
        public void SetBossSpawnPoint(Transform point)
        {
            eliteAndbossSpawnPoint = point;
        }

        private int instanceId;

        public void Initialize(RoomMeta meta, int floor = 1, int instanceId = 0)
        {
            this.instanceId = instanceId;
            roomMeta = meta;
            roomType = meta.RoomType;
            currentFloor = floor;
            currentState = RoomState.Inactive;

            ClearEnemies();
            UpdateEnemyCount();

            Log($"[RoomController] 初始化房间:  {meta.BundleName}, 类型: {roomType}, IsCombatRoom:  {IsCombatRoom}, InstanceId: {instanceId}");
        }

        public void ResetRoom()
        {
            ClearEnemies();
            currentState = RoomState.Inactive;
            UpdateEnemyCount();
            // 注意：不调用 roomPrefab.ResetVisited()，那是 RoomManager 的职责
        }


        /// <summary>
        /// 玩家进入房间
        /// </summary>
        /// <param name="entryDirection"></param>
        public void OnPlayerEnter(Direction entryDirection)
        {
            Log($"[RoomController] OnPlayerEnter, 方向: {entryDirection}, 当前状态: {currentState}, 房间类型: {roomType}");

            if (currentState != RoomState.Inactive)
            {
                Log("[RoomController] 房间已激活，跳过");
                return;
            }

            currentState = RoomState.Idle;
            // 记录入口方向，后续在战斗结束时用于决定哪个门保持关闭
            lastEntryDirection = entryDirection;

            // 发布房间进入事件到 EventBus
            try
            {
                EventBus.Publish(new RoomEnteredEvent { RoomId = roomMeta?.Index ?? 0, InstanceId = instanceId, RoomType = roomType });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoomController] 发布 RoomEnteredEvent 失败: " + ex.Message);
            }

            Log($"[RoomController] IsCombatRoom:  {IsCombatRoom}, IsCleared: {IsCleared}");

            if (IsCombatRoom && !IsCleared)
            {
                Log("[RoomController] 战斗房间，开始战斗");
                StartCombat();
            }
            else
            {
                Log("[RoomController] 非战斗房间，开启所有门（入口门保持关闭）");
                // 使用 RoomPrefab 的便捷方法，打开除入口门外的所有门
                roomPrefab?.OpenAllExcept(entryDirection);
            }
        }


        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartCombat()
        {
            if (currentState == RoomState.Combat)
            {
                Log("[RoomController] 已在战斗中，跳过");
                return;
            }

            currentState = RoomState.Combat;

            Log("[RoomController] 战斗开始：将所有门设为 Locked（不可通行且以锁定视觉显示）");
            roomPrefab?.LockAllDoors();

            Log("[RoomController] 生成敌人");
            SpawnEnemies();

            // 记录初始敌人数，供房间清理时统计
            initialEnemyCount = activeEnemies.Count;

            // 发布战斗开始事件到 EventBus
            try
            {
                EventBus.Publish(new CombatStartedEvent { RoomId = roomMeta?.Index ?? 0, InstanceId = instanceId, RoomType = roomType });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoomController] 发布 CombatStartedEvent 失败: " + ex.Message);
            }

            Log($"[RoomController] 战斗开始！类型: {roomType}, 敌人:  {activeEnemies.Count}");
        }

        /// <summary>
        /// 敌人死亡回调
        /// </summary>
        /// <param name="enemy"></param>
        public void OnEnemyDeath(GameObject enemy)
        {
            if (!activeEnemies.Contains(enemy)) return;
            UnsubscribeEnemyEvents(enemy);
            activeEnemies.Remove(enemy);
            UpdateEnemyCount();

            OnEnemyKilled?.Invoke(this, activeEnemies.Count);

            Log($"[RoomController] 敌人死亡，剩余: {activeEnemies.Count}");

            if (activeEnemies.Count <= 0 && currentState == RoomState.Combat)
            {
                CompleteCombat();
            }
        }

        /// <summary>
        /// 敌人死亡，带上击杀者（可能为 null），供玩家管理器或其他系统处理
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="attacker"></param>
        public void OnEnemyDeathWithKiller(GameObject enemy, GameObject attacker)
        {
            if (!activeEnemies.Contains(enemy)) return;
            UnsubscribeEnemyEvents(enemy);
            activeEnemies.Remove(enemy);
            UpdateEnemyCount();

            OnEnemyKilled?.Invoke(this, activeEnemies.Count);

            Log($"[RoomController] 敌人死亡（带击杀者）: {enemy.name}, 击杀者: {attacker?.name ?? "null"}, 剩余: {activeEnemies.Count}");

            // 由 EnemyCharacter 自行处理（掉落/能量），避免重复分配

            if (activeEnemies.Count <= 0 && currentState == RoomState.Combat)
            {
                CompleteCombat();
            }
        }

        [ContextMenu("Force Complete")]
        public void ForceComplete()
        {
            ClearEnemies();
            CompleteCombat();
        }

        /// <summary>
        /// 完成战斗
        /// </summary>
        private void CompleteCombat()
        {
            Log("[RoomController] 战斗完成！");

            currentState = RoomState.Cleared;

            Log("[RoomController] 战斗结束：开启所有门（入口门保持关闭）");
            roomPrefab?.OpenAllExcept(lastEntryDirection);

            // 生成房间奖励（简单实现）
            // TODO-完善奖励系统
            try
            {
                SpawnRewards();
            }
            catch { }

            // 发布房间清理事件到 EventBus，并传递清理的敌人数量
            int cleared = initialEnemyCount;
            try
            {
                EventBus.Publish(new RoomClearedEvent { RoomId = roomMeta?.Index ?? 0, InstanceId = instanceId, RoomType = roomType, ClearedEnemyCount = cleared });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoomController] 发布 RoomClearedEvent 失败: " + ex.Message);
            }

            // 重置所有玩家的技能使用状态，准备进入下一个房间
            try
            {
                ResetPlayerSkillsForRoomTransition();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoomController] 重置玩家技能失败: " + ex.Message);
            }

            Log($"[RoomController] 房间已清理！类型: {roomType}");
        }


        /// <summary>
        /// 生成敌人
        /// </summary>
        private void SpawnEnemies()
        {
            switch (roomType)
            {
                case RoomType.Normal:
                    SpawnNormalEnemies();
                    break;
                case RoomType.Elite:
                    SpawnEliteEnemies();
                    break;
                case RoomType.Boss:
                    SpawnBoss();
                    break;
                default:
                    Log($"[RoomController] 房间类型 {roomType} 不需要生成敌人");
                    CompleteCombat();
                    break;
            }

            UpdateEnemyCount();
        }

        /// <summary>
        /// 生成普通房间的敌人
        /// </summary>
        private void SpawnNormalEnemies()
        {
            if (enemySpawnConfig == null)
            {
                Log("[RoomController] 没有敌人配置，直接完成");
                CompleteCombat();
                return;
            }

            if(!IsCombatRoom) return;

            int count = UnityEngine.Random.Range(minEnemies, maxEnemies + 1);

            for (int i = 0; i < count; i++)
            {
                var prefab = enemySpawnConfig.SelectEnemy(enemySpawnConfig.normalEnemies, currentFloor);
                if (prefab != null)
                {
                    SpawnEnemy(prefab, GetSpawnPosition(i));
                }
            }

            if (activeEnemies.Count == 0)
            {
                Log("[RoomController] 没有生成任何敌人，直接完成");
                CompleteCombat();
            }
        }

        /// <summary>
        /// 生成精英房间的敌人
        /// </summary>
        private void SpawnEliteEnemies()
        {
            if (enemySpawnConfig == null)
            {
                CompleteCombat();
                return;
            }

            var elitePrefab = enemySpawnConfig.SelectEnemy(enemySpawnConfig.eliteEnemies, currentFloor);
            if (elitePrefab != null)
            {
                Vector3 elitePos = eliteAndbossSpawnPoint != null ?
                    eliteAndbossSpawnPoint.position : transform.position;
                SpawnEnemy(elitePrefab, elitePos);
            }

            int normalCount = UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < normalCount; i++)
            {
                var prefab = enemySpawnConfig.SelectEnemy(enemySpawnConfig.normalEnemies, currentFloor);
                if (prefab != null)
                {
                    SpawnEnemy(prefab, GetSpawnPosition(i));
                }
            }

            if (activeEnemies.Count == 0)
            {
                CompleteCombat();
            }
        }

        /// <summary>
        /// 生成 Boss 房间的敌人
        /// </summary>
        private void SpawnBoss()
        {
            if (enemySpawnConfig == null)
            {
                CompleteCombat();
                return;
            }

            var bossPrefab = enemySpawnConfig.SelectEnemy(enemySpawnConfig.bosses, currentFloor);
            if (bossPrefab != null)
            {
                Vector3 bossPos = eliteAndbossSpawnPoint != null ?
                    eliteAndbossSpawnPoint.position : transform.position;
                SpawnEnemy(bossPrefab, bossPos);
            }
            else
            {
                Log("[RoomController] 没有 Boss 预制体！");
                CompleteCombat();
            }
        }

        private void SpawnEnemy(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;

            // 使用 EnemyFactory 负责实例化与可选配置注入

            var enemy = RogueGame.Map.Factory.EnemyFactory.Spawn(prefab, position, transform, null, currentFloor);
            if (enemy == null) return;

            activeEnemies.Add(enemy);

            var health = enemy.GetComponent<HealthComponent>();
            if (health != null)
            {
                // 使用具名转发器，便于取消订阅与调试
                var forwarder = new EnemyDeathForwarder(this, enemy);
                Action deathHandler = forwarder.OnDeath;
                Action<GameObject> deathWithAttackerHandler = forwarder.OnDeathWithAttacker;
                health.OnDeath += deathHandler;
                health.OnDeathWithAttacker += deathWithAttackerHandler;
                _enemyDeathHandlersDic[enemy] = deathHandler;
                _enemyDeathWithAttackerHandlersDic[enemy] = deathWithAttackerHandler;
            }
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
            {
                var point = enemySpawnPoints[index % enemySpawnPoints.Length];
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
                return point.position + new Vector3(offset.x, offset.y, 0);
            }

            Vector2 randomPos = UnityEngine.Random.insideUnitCircle * 3f;
            return transform.position + new Vector3(randomPos.x, randomPos.y, 0);
        }

        private void ClearEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    UnsubscribeEnemyEvents(enemy);
                    Destroy(enemy);
                }
            }
            activeEnemies.Clear();
            UpdateEnemyCount();
        }

        #region Enemy Event 订阅
        private void UnsubscribeEnemyEvents(GameObject enemy)
        {
            if (enemy == null) return;
            var health = enemy.GetComponent<HealthComponent>();
            if (health == null) return;

            if (_enemyDeathHandlersDic.TryGetValue(enemy, out var dh))
            {
                health.OnDeath -= dh;
                _enemyDeathHandlersDic.Remove(enemy);
            }
            if (_enemyDeathWithAttackerHandlersDic.TryGetValue(enemy, out var dha))
            {
                health.OnDeathWithAttacker -= dha;
                _enemyDeathWithAttackerHandlersDic.Remove(enemy);
            }
        }

        #endregion

        private void UpdateEnemyCount()
        {
            enemyCount = activeEnemies.Count;
        }

        private void OnDestroy()
        {
            // Ensure all event subscriptions are cleaned up when controller is destroyed
            ClearEnemies();
        }

        // ========== 奖励 ==========

        private void SpawnRewards()
        {
            // 简单奖励实现：基于房间类型掉落固定金币
            int min = 0, max = 0;
            switch (roomType)
            {
                case RoomType.Normal:
                    min = 5; max = 10; break;
                case RoomType.Elite:
                    min = 15; max = 20; break;
                case RoomType.Boss:
                    min = 20; max = 30; break;
                default:
                    return;
            }

            int amount = UnityEngine.Random.Range(min, max + 1);
            try
            {
                LootDropper.Instance?.DropCoins(transform.position, amount);
                Log($"[RoomController] 掉落金币: {amount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RoomController] SpawnRewards failed: {ex.Message}");
            }
        }


        #region Editor And Debug
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (enemySpawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (var point in enemySpawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                    }
                }
            }

            if (eliteAndbossSpawnPoint != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(eliteAndbossSpawnPoint.position, 1f);
                UnityEditor.Handles.Label(eliteAndbossSpawnPoint.position + Vector3.up, "Boss");
            }

            string stateText = $"State: {currentState}\nType: {roomType}\nEnemies: {enemyCount}\nCanLeave: {CanPlayerLeave}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, stateText);
        }

        // 注意：已移除自动查找出生点的实现，改为手动配置或通过 SetEnemySpawnPoints 设置。
        // ========== 工具 ==========

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// 重置所有玩家的技能使用状态，为进入下一个房间做准备
        /// </summary>
        private void ResetPlayerSkillsForRoomTransition()
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.ResetSkillUsageForAllPlayers();
                Log("[RoomController] 已重置所有玩家技能使用状态，准备进入下一个房间");
            }
            else
            {
                Debug.LogWarning("[RoomController] PlayerManager.Instance 为空，无法重置技能");
            }
        }
#endif

        #endregion
    }
}