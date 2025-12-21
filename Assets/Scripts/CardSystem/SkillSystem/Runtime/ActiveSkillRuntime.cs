
using System;
using UnityEngine;

[Serializable]
public class ActiveSkillRuntime
{
    public string CardId;
    public SkillDefinition Skill;

    // 链接到 InventoryManager 中的 ActiveCardState.instanceId
    public string InstanceId;
    public float LastUseTime;
    public bool UsedInCurrentRoom;
    // 运行时协程引用（非序列化）用于取消延迟/选择流程
    [System.NonSerialized]
    public Coroutine RunningCoroutine;

    public ActiveSkillRuntime(string cardId, SkillDefinition skill, string instanceId)
    {
        CardId = cardId;
        Skill = skill;
        InstanceId = instanceId;
        LastUseTime = -999f;
        UsedInCurrentRoom = false;
    }
}