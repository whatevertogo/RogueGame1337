using UnityEngine;

namespace RogueGame.Map
{
    /// <summary>
    /// 未探索占位房间（Stub），仅用于显示出口与选择下一房。
    /// </summary>
    public class RoomStub : MonoBehaviour
    {
        public Direction Direction; // 面向/所属方向
        public Transform Anchor;    // 替换时的对齐点（可选）
    }
}
