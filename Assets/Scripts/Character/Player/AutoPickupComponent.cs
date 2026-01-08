using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 自动拾取组件（OverlapCircle 扫描实现，避免 trigger 互相干扰）
/// Periodically scans nearby pickup LayerMask，并调用 IPickup.OnEnterPickupRange
/// </summary>
public class AutoPickupComponent : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 2.5f;
    [SerializeField] private LayerMask pickupLayer;

    [Header("Scan")]
    [SerializeField] private float scanInterval = 0.12f;

    private readonly Collider2D[] hitBuffer = new Collider2D[32];
    private readonly HashSet<int> notified = new HashSet<int>();
    private ContactFilter2D pickupFilter;
    private float nextScanTime = 0f;

    private void Awake()
    {
        if (pickupLayer == 0)
        {
            int layer = LayerMask.NameToLayer("Pickups");
            if (layer != -1) pickupLayer = 1 << layer;
        }
        pickupFilter = new ContactFilter2D();
        pickupFilter.SetLayerMask(pickupLayer);
        pickupFilter.useTriggers = true;
    }

    private void Update()
    {
        if (Time.time < nextScanTime) return;
        nextScanTime = Time.time + Mathf.Max(0.01f, scanInterval);

        int count = Physics2D.OverlapCircle(
            transform.position, pickupRadius, pickupFilter, hitBuffer);

        var seenThisFrame = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            var col = hitBuffer[i];
            if (col == null) continue;
            int id = col.gameObject.GetInstanceID();
            seenThisFrame.Add(id);
            if (notified.Contains(id)) continue;

            var behaviours = col.GetComponentsInParent<MonoBehaviour>();
            foreach (var b in behaviours)
            {
                if (b is IPickup pickup)
                {
                    pickup.OnEnterPickupRange(this);
                    notified.Add(id);
                    break;
                }
            }
        }

        // cleanup notified entries not seen this frame
        var toRemove = new List<int>();
        foreach (var id in notified)
        {
            if (!seenThisFrame.Contains(id)) toRemove.Add(id);
        }
        foreach (var id in toRemove) notified.Remove(id);
    }

    public void IncreaseRadius(float amount)
    {
        pickupRadius += amount;
    }

    private void OnDisable()
    {
        notified.Clear();
    }
}
