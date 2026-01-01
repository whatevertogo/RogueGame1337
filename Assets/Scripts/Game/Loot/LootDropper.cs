using System;
using UnityEngine;
using CDTU.Utils;

/// <summary>
/// LootDropper：负责在地图上实例化拾取预制体（硬币/卡牌）
/// 单例——在场景中放置一个并为其分配预制体。
/// </summary>
public sealed class LootDropper : Singleton<LootDropper>
{
    [Header("Prefabs")]
    public GameObject CoinPickupPrefab;
    public GameObject CardPickupPrefab;

    /// <summary>
    /// 在指定位置掉落一个带有指定数量的硬币拾取物
    /// </summary>
    public void DropCoins(Vector3 position, int amount)
    {
        if (CoinPickupPrefab == null)
        {
            CDTU.Utils.CDLogger.LogWarning("LootDropper: CoinPickupPrefab not assigned");
            return;
        }
        var go = Instantiate(CoinPickupPrefab, position, Quaternion.identity);
        SetPickupLayer(go);
        var cp = go.GetComponent<CoinPickup>();
        if (cp != null)
        {
            cp.Amount = Mathf.Max(1, amount);
        }
        // give it a small push if physics present
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var dir = UnityEngine.Random.insideUnitCircle.normalized * 1.5f;
            rb.AddForce(dir, ForceMode2D.Impulse);
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 在指定位置掉落一个卡牌拾取物（主动或被动）
    /// </summary>
    /// <param name="position">掉落位置</param>
    /// <param name="cardId">卡牌ID</param>
    public void DropCard(Vector3 position, string cardId)
    {
        if (CardPickupPrefab == null)
        {
            CDTU.Utils.CDLogger.LogWarning("LootDropper: CardPickupPrefab not assigned");
            return;
        }
        if (string.IsNullOrEmpty(cardId)) return;
        var go = Instantiate(CardPickupPrefab, position, Quaternion.identity);
        SetPickupLayer(go);
        var cp = go.GetComponent<CardPickup>();
        if (cp != null)
        {
            cp.CardId = cardId;
        }
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var dir = UnityEngine.Random.insideUnitCircle.normalized * 1.2f;
            rb.AddForce(dir, ForceMode2D.Impulse);
        }
    }

    private void SetPickupLayer(GameObject go)
    {
        if (go == null) return;
        int layer = LayerMask.NameToLayer("Pickups");
        if (layer == -1) return;
        // set recursively
        SetLayerRecursive(go.transform, layer);
    }

    private void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
    }
}
