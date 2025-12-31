using System.Collections.Generic;
using Character;
using UnityEngine;

/// <summary>
/// 目标获取结果（由策略填充，供跨阶段修改器使用）
/// </summary>
public struct TargetResult
{
    /// 获取到的目标列表
    /// </summary>
    public List<CharacterBase> Targets;

    /// <summary>
    /// 目标点位置
    /// </summary>
    public Vector3 Point;
}