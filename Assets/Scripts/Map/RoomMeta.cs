using UnityEngine;
using RogueGame.Game.Service.SkillLimit;

namespace RogueGame.Map
{
    using UnityEngine;
    /// <summary>
    /// 房间元数据：描述类型、资源名、索引、标记与尺寸。
    /// </summary>
    [System.Serializable]
    public class RoomMeta : ISkillRuleProvider
    {
        public RoomType RoomType;
        public int Index;
        public string BundleName;
        public Direction AvailableExits = Direction.All;

        [Header("房间尺寸")]
        public float Width = 0f;
        public float Height = 0f;

        [Header("技能规则")]
        [Tooltip("该房间的技能使用规则")]
        [SerializeField]
        private RoomSkillRule skillRule = RoomSkillRule.OneTimePerRoom;

        public bool HasCustomSize => Width > 0 && Height > 0;

        // ISkillRuleProvider 实现
        public RoomSkillRule SkillRule => skillRule;

        public RoomMeta Clone()
        {
            return (RoomMeta)MemberwiseClone();
        }
    }


}