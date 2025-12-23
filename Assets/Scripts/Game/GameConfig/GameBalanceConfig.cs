using UnityEngine;
using RogueGame.Map;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Game/Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("技能充能配置")]
    [Tooltip("击杀普通敌人获得的充能")]
    public int normalEnemyCharge = 10;

    [Tooltip("击杀精英敌人获得的充能")]
    public int eliteEnemyCharge = 30;

    [Tooltip("击杀Boss获得的充能")]
    public int bossEnemyCharge = 50;

    /// <summary>根据房间类型获取充能值</summary>
    public int GetChargeForRoomType(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Normal => normalEnemyCharge,
            RoomType.Elite => eliteEnemyCharge,
            RoomType.Boss => bossEnemyCharge,
            _ => normalEnemyCharge
        };
    }
}