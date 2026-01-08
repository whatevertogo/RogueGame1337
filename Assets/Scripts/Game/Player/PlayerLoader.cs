

using UnityEngine;

/// <summary>
/// 玩家加载器：从 Resources 加载玩家预制体
/// </summary>
public sealed class PlayerLoader
{

    private const string DefaulPathfix = "Players";

    public GameObject Load(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return null;
        var path = DefaulPathfix + "/" + playerId;
        return Resources.Load<GameObject>(path);
    }



}