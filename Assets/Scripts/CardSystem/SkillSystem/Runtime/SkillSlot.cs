using UnityEngine;
[System.Serializable]
public class SkillSlot
{
    // 当前槽位的运行时数据（可能为 null）
    public ActiveSkillRuntime Runtime;

    public bool IsEmpty => Runtime == null;

    public void Equip(ActiveSkillRuntime runtime)
    {
        Runtime = runtime;
    }

    public void Clear()
    {
        Runtime = null;
    }
}
