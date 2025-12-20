using System.Collections.Generic;
using UnityEngine;
using RogueGame.Map.Loading;
using RogueGame.Map.Data;
using System;
using System.Collections;
using Character.Interfaces;
using RogueGame.Events;

namespace RogueGame.Map
{
    /// <summary>
    /// 房间管理器 - 房间生成后永久保留，不使用对象池
    /// </summary>
    public sealed class RoomManager : MonoBehaviour, IRoomManager
    {
        #region 配置

        [Header("房间配置"), InlineEditor]
        [SerializeField] private RoomWeightTable weightTable;
        [InlineEditor]
        [SerializeField] private List<RoomVariantSet> variantSets;
        [SerializeField] private int roomsToUnlockBoss = 6;
        [SerializeField] private int seed = 12345;

        [Header("预制体")]
        [SerializeField] private Transform roomsRoot;

        [Header("房间布局")]
        [SerializeField] private float defaultRoomWidth = 30f;
        [SerializeField] private float defaultRoomHeight = 30f;
        [SerializeField] private float roomGap = 2f;

        // 相机与传送的配置已迁移到 TransitionController

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = true;

        #endregion

        #region 运行时状态

        private RoomSelector _selector;
        private IRoomLoader _loader;

        private int _nextInstanceId = 1;

        private RoomInstanceState _current;
        private readonly List<RoomInstanceState> _allRooms = new(); // 所有已生成的房间
        private readonly List<GameObject> _activeStubs = new();
        private readonly HashSet<DoorController> _subscribedDoors = new();

        private int _currentFloor = 1;
        private bool _isSwitchingRoom;

        // 缓存 TransitionController 引用（由 GameManager 提供或场景中查找）
        private TransitionController TransitionController;

        #endregion

        // 事件

        public event Action<RoomController> OnRoomCleared;
        public event Action<string> OnShowMessage;
        // 门触发请求已用 EventBus 发布

        // 只读属性
        public RoomInstanceState CurrentRoom => _current;
        public int CurrentFloor => _currentFloor;
        // 注意: 层级计数与 Boss 解锁由 GameStateManager 管理，RoomManager 仅保留当前层号作信息用途
        public Vector2 CurrentRoomSize => _current?.CachedSize ?? new Vector2(defaultRoomWidth, defaultRoomHeight);
        public RoomController CurrentRoomController => _current?.Instance?.GetComponent<RoomController>();


        #region 生命周期

        private void Awake()
        {
            _loader = new ResourcesRoomLoader();
            if (TransitionController == null)
            {
                CDTU.Utils.Logger.LogWarning("[RoomManager] TransitionController not found in scene.");
            }
            // RoomManager 不再订阅全局流程事件，流程由 GameStateManager 统一控制。
            // 订阅 EventBus，以响应 RoomController 发布的房间级事件
            EventBus.Subscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
            EventBus.Subscribe<CombatStartedEvent>(HandleCombatStartedEvent);
            EventBus.Subscribe<RoomClearedEvent>(HandleRoomClearedEvent);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<RoomEnteredEvent>(HandleRoomEnteredEvent);
            EventBus.Unsubscribe<CombatStartedEvent>(HandleCombatStartedEvent);
            EventBus.Unsubscribe<RoomClearedEvent>(HandleRoomClearedEvent);
        }

        #endregion



        #region 公共 API

        public void Initialize(TransitionController transitionController)
        {
            this.TransitionController = transitionController;
        }

        public int RandomSeed() => (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
        public void StartFloor(int floor, RoomMeta startMeta)
        {
            _currentFloor = floor;
            StartRun(startMeta);
        }
        
        public void StartRun(RoomMeta startMeta)
        {
            ClearAll();
            ResetRunState();
            RandomSeed();
            _selector = new RoomSelector(weightTable, BuildVariantDict(), seed);
            SpawnAndEnter(startMeta, Direction.None);
        }


        public int GetBossUnlockThreshold()
        {
            if (weightTable != null)
            {
                try { return weightTable.BossAfterRooms; } catch { }
            }
            return roomsToUnlockBoss;
        }

        public bool TryEnterDoor(Direction dir)
        {
            if (_isSwitchingRoom)
            {
                Log("[RoomManager] 正在切换房间中");
                return false;
            }

            var controller = CurrentRoomController;
            if (controller != null && !controller.CanPlayerLeave)
            {
                string reason = controller.GetCannotLeaveReason();
                Log($"[RoomManager] 无法离开:  {reason}");
                OnShowMessage?.Invoke(reason);
                return false;
            }

            // 不直接执行切换；将请求发布到 EventBus，由 GameStateManager 负责执行过渡
            try
            {
                EventBus.Publish(new DoorEnterRequestedEvent { Direction = dir, RoomId = _current?.Meta?.Index ?? 0, InstanceId = _current?.InstanceId ?? 0 });
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[RoomManager] 发布 DoorEnterRequestedEvent 失败: " + ex.Message);
            }
            return true;
        }

        // 获得当前房间位置
        public Vector2 GetCurrentRoomPosition()
        {
            return _current?.Instance.transform.position ?? Vector2.zero;
        }

        // IReadOnlyRoomRepository 实现 - 只读查询接口
        public RoomInstanceState GetInstance(int instanceId)
        {
            return _allRooms.Find(r => r.InstanceId == instanceId);
        }

        public bool TryGetInstance(int instanceId, out RoomInstanceState instance)
        {
            instance = GetInstance(instanceId);
            return instance != null;
        }

        public IEnumerable<RoomInstanceState> GetAllInstances()
        {
            return _allRooms.ToArray();
        }

        public IEnumerable<RoomInstanceState> GetInstancesOnFloor(int floor)
        {
            return _allRooms.FindAll(r => r != null && r.Floor == floor).ToArray();
        }

        public IEnumerable<RoomInstanceState> GetUnvisitedOnFloor(int floor)
        {
            var list = new List<RoomInstanceState>();
            foreach (var r in _allRooms)
            {
                if (r == null || r.Floor != floor) continue;
                var available = r.Meta?.AvailableExits ?? Direction.None;
                // 若存在至少一个出口未访问，则视为未访问
                if (((available) & (~r.VisitedMask)) != Direction.None)
                {
                    list.Add(r);
                }
            }
            return list.ToArray();
        }

        #endregion

        #region 公开api房间切换核心逻辑
        // 对外暴露切换实现，注意：调用方应确保不会重入（RoomManager 也有内部保护）
        public void SwitchToNextRoom(Direction exitDir)
        {
            if (_current?.Meta == null) return;

            _isSwitchingRoom = true;

            try
            {
                var nextMeta = _selector.NextRoom(_current.Meta.RoomType);
                var entryDir = DirectionUtils.Opposite(exitDir);

                UnsubscribeCurrentRoomDoors();

                // 不销毁旧房间，只是不再是当前房间
                SpawnAndEnter(nextMeta, entryDir);
            }
            finally
            {
                _isSwitchingRoom = false;
            }
        }

        private void SpawnAndEnter(RoomMeta meta, Direction entryDir)
        {
            if (meta == null) return;

            // 直接加载并实例化（不使用对象池）
            var prefab = _loader.Load(meta);
            if (prefab == null)
            {
                Debug.LogError($"[RoomManager] 无法加载房间:  {meta.BundleName}");
                return;
            }

            var go = Instantiate(prefab, roomsRoot);
            go.name = meta.BundleName;

            var roomPrefab = go.GetComponent<RoomPrefab>();
            var roomController = go.GetComponent<RoomController>();

            if (roomPrefab == null)
            {
                Debug.LogError($"[RoomManager] 房间缺少 RoomPrefab:  {meta.BundleName}");
                Destroy(go);
                return;
            }

            // 初始化
            int newInstanceId = _nextInstanceId++;
            InitializeRoomController(roomController, meta, newInstanceId);

            // 计算位置
            Vector2 roomSize = GetRoomSize(go, meta);
            Vector3 roomPos = CalculateRoomPosition(roomSize, entryDir);
            go.transform.localPosition = roomPos;

            // 创建状态
            var state = new RoomInstanceState
            {
                InstanceId = newInstanceId,
                Floor = _currentFloor,
                Meta = meta,
                Instance = go,
                WorldPosition = roomPos,
                CachedSize = roomSize
            };

            // 标记入口
            if (entryDir != Direction.None)
            {
                state.MarkVisited(entryDir);
            }

            //将入口的门标记为Locked
            roomPrefab.GetDoor(entryDir)?.Lock();


            // 保存到列表
            _allRooms.Add(state);
            _current = state;

            // 传送玩家（优先使用 TransitionController）
            if (TransitionController != null)
            {
                TransitionController.TeleportPlayer(roomPrefab, entryDir);
            }
            else
            {
                Debug.LogError("[RoomManager] 无 TransitionController：请在场景中添加 TransitionController 或在 GameManager 中注入。");
            }

            // 触发进入
            roomController?.OnPlayerEnter(entryDir);

            // 订阅门事件
            SubscribeToRoomDoors(roomPrefab);


            // 移动相机（由 TransitionController 承担）
            if (TransitionController != null)
            {
                TransitionController.MoveCameraTo(roomPos);
            }
            else
            {
                Debug.LogError("[RoomManager] 无 TransitionController：请在场景中添加 TransitionController 或在 GameManager 中注入。");
            }

            Log($"[RoomManager] 进入房间: {meta.BundleName}, 总房间数: {_allRooms.Count}");
        }

        private void InitializeRoomController(RoomController controller, RoomMeta meta, int instanceId)
        {
            if (controller == null) return;

            controller.Initialize(meta, _currentFloor, instanceId);

            // 仅保留敌人死亡的本地事件订阅；房间级生命周期事件使用 EventBus
            controller.OnEnemyKilled -= HandleEnemyKilled;
            controller.OnEnemyKilled += HandleEnemyKilled;
        }

        #endregion

        #region 门订阅管理

        private void SubscribeToRoomDoors(RoomPrefab room)
        {
            if (room == null) return;

            foreach (var door in room.Doors)
            {
                if (door == null || _subscribedDoors.Contains(door)) continue;

                door.OnPlayerEnterDoor += HandleDoorEntered;
                _subscribedDoors.Add(door);
            }
        }

        private void UnsubscribeCurrentRoomDoors()
        {
            foreach (var door in _subscribedDoors)
            {
                if (door != null)
                {
                    door.OnPlayerEnterDoor -= HandleDoorEntered;
                }
            }
            _subscribedDoors.Clear();
        }

        private void HandleDoorEntered(Direction dir)
        {
            TryEnterDoor(dir);
        }

        private void HandleRoomActivated(RoomController controller)
        {
            // 房间进入时重置技能使用标记，以便玩家在新房间可以再次使用技能
            // 改为由 PlayerManager 订阅 RoomEnteredEvent 负责重置，避免职责重复
            // 保持此处仅做日志/扩展点
            Log("[RoomManager] HandleRoomActivated: room activated");
        }

        private void HandleRoomEnteredEvent(RoomEnteredEvent evt)
        {
            if (_current == null) return;
            if (evt.InstanceId == _current.InstanceId)
            {
                // 触发本地激活逻辑
                HandleRoomActivated(CurrentRoomController);
            }
        }

        private void HandleCombatStartedEvent(CombatStartedEvent evt)
        {
            // 目前 RoomManager 不需要在战斗开始时执行额外逻辑，保留扩展点
        }

        private void HandleRoomClearedEvent(RoomClearedEvent evt)
        {
            // 找到对应房间并执行本地清理逻辑
            var state = _allRooms.Find(r => r.InstanceId == evt.InstanceId);
            var controller = state?.Instance?.GetComponent<RoomController>();
            if (controller != null)
            {
                HandleRoomCleared(controller);
            }
        }

        #endregion


        #region 房间事件处理

        private void HandleRoomCleared(RoomController room)
        {
            // 仅做本地回调与日志；层级统计与 Boss 解锁由 GameStateManager 负责
            OnRoomCleared?.Invoke(room);
            Log($"[RoomManager] 房间清理完成, 房间类型: {room?.RoomType}, InstanceId: {room?.RoomMeta?.Index}");
        }

        // ========== EventBus 事件处理（最小） ==========
        // 已移除对全局事件的处理，流程由 GameStateManager 负责

        private void HandleEnemyKilled(RoomController room, int remaining)
        {
            Log($"[RoomManager] 敌人击杀，剩余: {remaining}");
        }

        #endregion

        #region 清理

        private void ClearAll()
        {
            UnsubscribeCurrentRoomDoors();

            // 销毁所有房间
            foreach (var state in _allRooms)
            {
                if (state?.Instance != null)
                {
                    Destroy(state.Instance);
                }
            }
            _allRooms.Clear();
            _current = null;
        }

        private void ResetRunState()
        {
            _isSwitchingRoom = false;
        }

        #endregion

        #region 工具方法

        private Vector2 GetRoomSize(GameObject roomGO, RoomMeta meta)
        {
            var roomPrefab = roomGO?.GetComponent<RoomPrefab>();
            if (roomPrefab != null)
            {
                var size = roomPrefab.GetSize();
                if (size.x > 0 && size.y > 0) return size;
            }

            if (meta?.HasCustomSize == true)
            {
                return new Vector2(meta.Width, meta.Height);
            }

            return new Vector2(defaultRoomWidth, defaultRoomHeight);
        }

        /// <summary>
        /// 计算新房间位置
        /// </summary>
        /// <param name="newSize"></param>
        /// <param name="entryDir"></param>
        /// <returns></returns>
        private Vector3 CalculateRoomPosition(Vector2 newSize, Direction entryDir)
        {
            if (_current == null) return Vector3.zero;

            var currentSize = _current.CachedSize;
            var offset = entryDir switch
            {
                Direction.North => new Vector3(0, (currentSize.y + newSize.y) / 2f + roomGap, 0),
                Direction.South => new Vector3(0, -(currentSize.y + newSize.y) / 2f - roomGap, 0),
                Direction.East => new Vector3((currentSize.x + newSize.x) / 2f + roomGap, 0, 0),
                Direction.West => new Vector3(-(currentSize.x + newSize.x) / 2f - roomGap, 0, 0),
                _ => Vector3.zero
            };

            return _current.WorldPosition + offset;
        }

        private Dictionary<RoomType, RoomVariantSet> BuildVariantDict()
        {
            var dict = new Dictionary<RoomType, RoomVariantSet>();
            if (variantSets == null) return dict;

            foreach (var v in variantSets)
            {
                if (v != null) dict[v.RoomType] = v;
            }
            return dict;
        }

        private static IEnumerable<Direction> GetDirections(Direction mask)
        {
            if ((mask & Direction.North) != 0) yield return Direction.North;
            if ((mask & Direction.East) != 0) yield return Direction.East;
            if ((mask & Direction.South) != 0) yield return Direction.South;
            if ((mask & Direction.West) != 0) yield return Direction.West;
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log(message);
            }
        }

        #endregion

    }
}