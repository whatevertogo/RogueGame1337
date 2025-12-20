using UnityEngine;

[RequireComponent(typeof(Collider2D))]

/// <summary>
/// Card pickup: supports auto-pickup magnet and direct collision
/// </summary>
public sealed class CardPickup : MonoBehaviour, IPickup
{
    public string CardId;
    public float FlySpeed = 6f;
    public float PickupDistance = 0.03f;

    private Transform target;
    private bool isFlying = false;
    private bool _collected = false;

    private void Awake()
    {
        // 如果场景/预设没有设置 pickups 层, 则尝试自动设置
        int layer = LayerMask.NameToLayer("Pickups");
        if (layer != -1 && gameObject.layer == 0)
        {
            gameObject.layer = layer;
        }
    }

    public void OnEnterPickupRange(AutoPickupComponent picker)
    {
        if (isFlying) return;
        target = picker?.transform;
        if (target != null)
        {
            isFlying = true;
        }
    }

    private void Update()
    {
        if (_collected) return;
        if (!isFlying || target == null) return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, FlySpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target.position) < PickupDistance)
        {
            Pickup();
        }
    }

    private void Pickup()
    {
        if (_collected) return;
        _collected = true;
        if (string.IsNullOrEmpty(CardId))
        {
            Debug.LogWarning($"[CardPickup] pickup with empty CardId on '{gameObject.name}'");
            Destroy(gameObject);
            return;
        }

        // 查找卡牌定义
        if (!GameRoot.Instance.CardDatabase.TryResolve(CardId, out var data))
        {
            Debug.LogWarning($"[CardPickup] unknown CardId '{CardId}' on '{gameObject.name}', discarding pickup.");
            Destroy(gameObject);
            return;
        }
        //TODO -完善添加卡牌逻辑
        // InventoryManager.Instance.AddCardById(CardId);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        if (_collected) return;
        Pickup();
    }
}
