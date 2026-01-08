using System.Collections.Generic;
using Core.Events; // EventBus
using UnityEngine;

namespace Character.Player
{
    /// <summary>
    /// 玩家交互检测组件，负责管理进入交互范围内的可交互对象
    /// </summary>
    public class PlayerInteractorComponent : MonoBehaviour
    {
        private HashSet<IInteractable> _targets = new HashSet<IInteractable>();
        private IInteractable _bestTarget;

        void Update()
        {
            FindBestTarget();

            // 检测按下瞬间而非按住状态
            if (GameInput.Instance.InteractPressedThisFrame && _bestTarget != null)
            {
                _bestTarget.Interact(gameObject);
            }
        }

        void FindBestTarget()
        {
            _bestTarget = null;
            if (_targets.Count == 0)
                return;

            Vector3 selfPos = transform.position;
            float minSqr = float.MaxValue;

            foreach (var target in _targets)
            {
                if (target is MonoBehaviour mono)
                {
                    float sqr = (mono.transform.position - selfPos).sqrMagnitude;
                    if (sqr < minSqr)
                    {
                        minSqr = sqr;
                        _bestTarget = target;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var it = other.GetComponent<IInteractable>();
            if (it != null)
            {
                if (_targets.Add(it))
                    it.OnPlayerEnter(this.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var it = other.GetComponent<IInteractable>();
            if (it != null)
            {
                if (_targets.Remove(it))
                    it.OnPlayerExit(this.gameObject);

                if (_bestTarget == it)
                    _bestTarget = null;
            }
        }

        private void OnDisable()
        {
            // 先重置目标引用，避免清理期间被访问
            _bestTarget = null;

            // 安全清理：捕获异常防止中断
            foreach (var it in _targets)
            {
                it.OnPlayerExit(this.gameObject);
            }
            _targets.Clear();
        }
    }
}
