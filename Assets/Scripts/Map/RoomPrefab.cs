using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RogueGame.Map
{
    /// <summary>
    /// 房间预制体组件 - 管理房间结构和门
    /// </summary>
    public class RoomPrefab : MonoBehaviour
    {
        [Header("出生点")]
        public Transform PlayerSpawn;

        [Header("门")]
        [SerializeField] private DoorController doorNorth;
        [SerializeField] private DoorController doorSouth;
        [SerializeField] private DoorController doorEast;
        [SerializeField] private DoorController doorWest;

        [Header("房间尺寸")]
        [SerializeField] private bool autoCalculateSize = true;
        [SerializeField] private float manualWidth = 20f;
        [SerializeField] private float manualHeight = 20f;
        [SerializeField] private float sizePadding = 0f;

        [Header("房间属性")]
        [SerializeField] private bool isSafeRoom = false;

        // 缓存
        private Vector2 _cachedSize = Vector2.zero;
        private bool _sizeCalculated = false;
        private List<DoorController> _allDoors;

        public bool IsSafeRoom => isSafeRoom;

        // ========== 门访问 ==========

        public IReadOnlyList<DoorController> Doors
        {
            get
            {
                if (_allDoors == null)
                {
                    _allDoors = new List<DoorController>();
                    if (doorNorth != null) _allDoors.Add(doorNorth);
                    if (doorSouth != null) _allDoors.Add(doorSouth);
                    if (doorEast != null) _allDoors.Add(doorEast);
                    if (doorWest != null) _allDoors.Add(doorWest);
                }
                return _allDoors;
            }
        }

        public DoorController GetDoor(Direction dir)
        {
            return dir switch
            {
                Direction.North => doorNorth,
                Direction.South => doorSouth,
                Direction.East => doorEast,
                Direction.West => doorWest,
                _ => null
            };
        }

        public bool HasDoor(Direction dir) => GetDoor(dir) != null;

        // ========== 位置获取 ==========

        public Vector3 GetDoorPosition(Direction dir)
        {
            var door = GetDoor(dir);
            if (door != null)
                return door.DoorPosition;

            var size = GetSize();
            return transform.position + dir switch
            {
                Direction.North => new Vector3(0, size.y / 2f, 0),
                Direction.South => new Vector3(0, -size.y / 2f, 0),
                Direction.East => new Vector3(size.x / 2f, 0, 0),
                Direction.West => new Vector3(-size.x / 2f, 0, 0),
                _ => Vector3.zero
            };
        }



        public Vector3 GetExitPosition(Direction dir)
        {
            var door = GetDoor(dir);
            if (door != null)
                return door.ExitPosition;

            return PlayerSpawn != null ? PlayerSpawn.position : transform.position;
        }

        public Transform GetAnchor(Direction dir)
        {
            var door = GetDoor(dir);
            return door != null ? door.transform : null;
        }

        // ========== 门状态控制 ==========

        public void OpenAllDoors()
        {
            Debug.Log($"[RoomPrefab] OpenAllDoors 调用，门数量: {Doors.Count}");
            foreach (var door in Doors)
            {
                if (door != null)
                {
                    door.Open();
                }
            }
        }

        public void CloseAllDoors()
        {
            Debug.Log($"[RoomPrefab] CloseAllDoors 调用，门数量: {Doors.Count}");
            foreach (var door in Doors)
            {
                if (door != null)
                {
                    door.Close();
                }
            }
        }

        /// <summary>
        /// 将房间内所有门设为 Locked（锁定状态）
        /// </summary>
        public void LockAllDoors()
        {
            Debug.Log($"[RoomPrefab] LockAllDoors 调用，门数量: {Doors.Count}");
            foreach (var door in Doors)
            {
                if (door != null)
                {
                    door.Lock();
                }
            }
        }

        /// <summary>
        /// 打开房间内所有门，除了指定的方向（该方向门将保持 Closed）
        /// </summary>
        public void OpenAllExcept(Direction excludeDir)
        {
            Debug.Log($"[RoomPrefab] OpenAllExcept 调用，排除方向: {excludeDir}");
            foreach (var door in Doors)
            {
                if (door == null) continue;

                if (excludeDir != Direction.None && door.Direction == excludeDir)
                {
                    door.Close();
                }
                else
                {
                    door.Open();
                }
            }
        }

        public void SetDoorVisible(Direction dir, bool visible)
        {
            var door = GetDoor(dir);
            if (door != null)
            {
                if (visible) door.Show();
                else door.Hide();
            }
        }

        // FullReset 保留以便对象池场景可以重置门的视觉/交互状态
        public void FullReset()
        {
            Debug.Log("[RoomPrefab] FullReset 调用（已移除访问标记逻辑，保留门重置）");
            foreach (var door in Doors)
            {
                door?.Reset();
            }
        }

        // ========== 尺寸计算 ==========

        public Vector2 GetSize()
        {
            if (!_sizeCalculated)
            {
                CalculateSize();
            }
            return _cachedSize;
        }

        public float GetWidth() => GetSize().x;
        public float GetHeight() => GetSize().y;

        public void RecalculateSize()
        {
            _sizeCalculated = false;
            CalculateSize();
        }

        /// <summary>
        /// 计算房间尺寸
        /// </summary>
        private void CalculateSize()
        {
            if (!autoCalculateSize)
            {
                _cachedSize = new Vector2(manualWidth, manualHeight);
                _sizeCalculated = true;
                return;
            }

            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool foundAny = false;

            // Tilemap
            var tilemaps = GetComponentsInChildren<Tilemap>();
            foreach (var tm in tilemaps)
            {
                tm.CompressBounds();
                if (tm.cellBounds.size == Vector3Int.zero) continue;

                var localBounds = tm.localBounds;
                var worldMin = tm.transform.TransformPoint(localBounds.min);
                var worldMax = tm.transform.TransformPoint(localBounds.max);

                if (!foundAny)
                {
                    bounds = new Bounds((worldMin + worldMax) / 2f, Vector3.zero);
                    foundAny = true;
                }
                bounds.Encapsulate(worldMin);
                bounds.Encapsulate(worldMax);
            }

            // Renderer
            if (!foundAny)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r is ParticleSystemRenderer) continue;

                    if (!foundAny)
                    {
                        bounds = r.bounds;
                        foundAny = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }
            }

            // Collider2D
            if (!foundAny)
            {
                var colliders = GetComponentsInChildren<Collider2D>();
                foreach (var col in colliders)
                {
                    if (!foundAny)
                    {
                        bounds = col.bounds;
                        foundAny = true;
                    }
                    else
                    {
                        bounds.Encapsulate(col.bounds);
                    }
                }
            }

            // 门位置
            if (!foundAny)
            {
                bounds = CalculateBoundsFromDoors();
                foundAny = bounds.size.magnitude > 0;
            }

            if (!foundAny || bounds.size.magnitude < 0.1f)
            {
                _cachedSize = new Vector2(manualWidth, manualHeight);
            }
            else
            {
                _cachedSize = new Vector2(
                    bounds.size.x + sizePadding * 2,
                    bounds.size.y + sizePadding * 2
                );
            }

            _sizeCalculated = true;
        }

        private Bounds CalculateBoundsFromDoors()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);

            foreach (var door in Doors)
            {
                if (door != null)
                {
                    bounds.Encapsulate(door.transform.position);
                }
            }

            if (bounds.size.magnitude > 0)
            {
                bounds.Expand(2f);
            }

            return bounds;
        }

        // ========== 编辑器 ==========

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var size = GetSize();

            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0));

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (size.y / 2f + 1f),
                $"Size: {size.x:F1} x {size.y:F1}"
            );

            if (PlayerSpawn != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(PlayerSpawn.position, 0.5f);
                UnityEditor.Handles.Label(PlayerSpawn.position + Vector3.up, "PlayerSpawn");
            }
        }

        [ContextMenu("Auto Find Doors")]
        private void AutoFindDoors()
        {
            var doors = GetComponentsInChildren<DoorController>();
            foreach (var door in doors)
            {
                switch (door.Direction)
                {
                    case Direction.North: doorNorth = door; break;
                    case Direction.South: doorSouth = door; break;
                    case Direction.East: doorEast = door; break;
                    case Direction.West: doorWest = door; break;
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}