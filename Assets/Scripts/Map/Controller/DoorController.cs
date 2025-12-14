using UnityEngine;
using System;

namespace RogueGame.Map
{
    /// <summary>
    /// 门控制器 - 出口锚点 + 交互触发器
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [Header("方向")]
        [SerializeField] private Direction direction;

        [Header("位置点")]
        [Tooltip("玩家从此门进入后的出生位置")]
        [SerializeField] private Transform exitPoint;

        [Tooltip("Stub 放置位置（门外侧）")]
        [SerializeField] private Transform stubPoint;

        [Header("视觉组件")]
        [SerializeField] private GameObject doorVisual;
        [SerializeField] private SpriteRenderer doorSprite;

        [Header("碰撞组件")]
        [SerializeField] private Collider2D doorBlocker;
        [SerializeField] private Collider2D doorTrigger;

        [Header("初始状态")]
        [SerializeField] private DoorState initialState = DoorState.Closed;

        [Header("当前状态（调试用）")]
        [SerializeField] private DoorState currentState = DoorState.Closed;

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = true;

        // 事件
        public event Action<Direction> OnPlayerEnterDoor;

        // ========== 属性 ==========

        public Direction Direction => direction;
        public DoorState CurrentState => currentState;
        public bool IsOpen => currentState == DoorState.Open;
        public bool IsPassable => currentState == DoorState.Open;

        public Vector3 DoorPosition => stubPoint != null ? stubPoint.position : transform.position;

        public Vector3 ExitPosition
        {
            get
            {
                if (exitPoint != null)
                    return exitPoint.position;

                Vector3 offset = direction switch
                {
                    Direction.North => Vector3.down * 1.5f,
                    Direction.South => Vector3.up * 1.5f,
                    Direction.East => Vector3.left * 1.5f,
                    Direction.West => Vector3.right * 1.5f,
                    _ => Vector3.zero
                };
                return transform.position + offset;
            }
        }

        // ========== 生命周期 ==========

        private void Awake()
        {
            AutoBindColliders();
            currentState = initialState;
            UpdateVisual();
        }

        private void AutoBindColliders()
        {
            var cols = GetComponents<Collider2D>();
            foreach (var c in cols)
            {
                if (c == null) continue;
                if (c.isTrigger)
                {
                    if (doorTrigger == null) doorTrigger = c;
                }
                else
                {
                    if (doorBlocker == null) doorBlocker = c;
                }
            }
        }

        // ========== 状态控制 ==========

        public void Open()
        {
            if (currentState == DoorState.Hidden) return;

            Log($"[Door-{direction}] Open() 调用，{currentState} → Open");
            currentState = DoorState.Open;
            UpdateVisual();
        }

        public void Close()
        {
            if (currentState == DoorState.Hidden) return;

            Log($"[Door-{direction}] Close() 调用，{currentState} → Closed");
            currentState = DoorState.Closed;
            UpdateVisual();
        }

        public void Lock()
        {
            Log($"[Door-{direction}] Lock() 调用");
            currentState = DoorState.Locked;
            UpdateVisual();
        }

        public void Hide()
        {
            Log($"[Door-{direction}] Hide() 调用");
            currentState = DoorState.Hidden;
            UpdateVisual();
        }

        public void Show()
        {
            if (currentState == DoorState.Hidden)
            {
                Log($"[Door-{direction}] Show() 调用");
                currentState = DoorState.Closed;
                UpdateVisual();
            }
        }

        /// <summary>
        /// 重置到初始状态（不是强制关闭！）
        /// </summary>
        public void Reset()
        {
            Log($"[Door-{direction}] Reset() 调用，重置到初始状态:  {initialState}");
            currentState = initialState;
            UpdateVisual();
        }

        /// <summary>
        /// 设置门状态（兼容旧接口）
        /// </summary>
        public void SetState(Direction dir, bool visited, bool open)
        {
            if (dir != direction) return;
            if (currentState == DoorState.Hidden) return;

            currentState = open ? DoorState.Open : DoorState.Closed;
            UpdateVisual();
        }

        // ========== 视觉更新 ==========

        private void UpdateVisual()
        {
            bool showVisual = currentState != DoorState.Hidden;
            bool blockPlayer = currentState != DoorState.Open;

            // 更新阻挡碰撞体
            if (doorBlocker != null)
            {
                doorBlocker.enabled = blockPlayer;
            }

            // 触发器始终开启（用于检测玩家）
            if (doorTrigger != null)
            {
                doorTrigger.enabled = showVisual;
            }

            // 更新视觉
            if (doorVisual != null)
            {
                doorVisual.SetActive(showVisual);
            }

            // 更新颜色
            if (doorSprite != null)
            {
                doorSprite.color = currentState switch
                {
                    DoorState.Open => Color.green,
                    DoorState.Closed => Color.gray,
                    DoorState.Locked => Color.red,
                    DoorState.Hidden => Color.clear,
                    _ => Color.white
                };
            }

            Log($"[Door-{direction}] UpdateVisual:  state={currentState}, blocker={blockPlayer}");
        }

        // ========== 交互 ==========

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            Log($"[Door-{direction}] 玩家进入触发区，当前状态: {currentState}");

            if (currentState != DoorState.Open)
            {
                Log($"[Door-{direction}] 门未开启，无法通过");
                return;
            }

            Log($"[Door-{direction}] 触发通过事件");
            OnPlayerEnterDoor?.Invoke(direction);
        }

        // ========== 工具 ==========

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log(message);
            }
        }
        #region  编辑器

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (direction != Direction.None)
            {
                gameObject.name = $"Door_{direction}";
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = currentState switch
            {
                DoorState.Open => Color.green,
                DoorState.Closed => Color.yellow,
                DoorState.Locked => Color.red,
                DoorState.Hidden => new Color(0.5f, 0.5f, 0.5f, 0.3f),
                _ => Color.white
            };
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ExitPosition, 0.3f);
            Gizmos.DrawLine(transform.position, ExitPosition);

            if (stubPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(stubPoint.position, 0.25f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, DirectionUtils.ToVector3(direction) * 1f);
        }
#endif
        #endregion
        
    }
}