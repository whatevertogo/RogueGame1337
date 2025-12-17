using System;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// 简单的可序列化卡牌 ID 引用，用于在 Inspector 中显示/编辑 cardId 列表
    /// </summary>
    [Serializable]
    public class CardIdReference
    {
        public string Id;
    }
}
