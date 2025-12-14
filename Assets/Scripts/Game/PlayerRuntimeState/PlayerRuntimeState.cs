
using System.Collections.Generic;

public class PlayerRuntimeState
{
    public string Id;
    public PlayerController Controller;
    public bool IsLocal;
    // Coins and passive card counts are shared via RunInventory; per-player storage removed
    // 已装备主动卡通过 SkillSlots. ActiveSkillCards 全局放在 RunInventory
    public SkillSlotState[] SkillSlots = new SkillSlotState[2] { new SkillSlotState(), new SkillSlotState() };
    // 注意：事件处理器引用已移至 PlayerController（更适合与 GameObject 生命周期绑定）
}
