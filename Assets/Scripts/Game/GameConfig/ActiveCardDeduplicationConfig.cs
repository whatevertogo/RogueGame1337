using UnityEngine;

/// <summary>
/// 主动卡去重配置
/// 定义重复主动卡转换为金币的规则
/// </summary>
[ManagedData("Game")]
[CreateAssetMenu(fileName = "ActiveCardDeduplication", menuName = "RogueGame/Game/Active Card Deduplication")]
public class ActiveCardDeduplicationConfig : ScriptableObject
{
    [Header("金币转换配置")]
    [Tooltip("重复主动卡转换为金币的数量")]
    public int duplicateToCoins = 50;

    [Tooltip("是否启用去重功能")]
    public bool enableDeduplication = true;

    [Tooltip("是否在日志中显示去重信息")]
    public bool showDeduplicationLog = true;
}
