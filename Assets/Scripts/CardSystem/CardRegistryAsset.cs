using System.Collections.Generic;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// 可序列化的卡牌注册缓存（编辑器生成），便于将卡牌定义打包到 Resources 以加速运行时解析。
    /// </summary>
    [CreateAssetMenu(menuName = "CardSystem/CardRegistryCache")]
    public class CardRegistryAsset : ScriptableObject
    {
        public List<CardData> definitions = new List<CardData>();
    }
}
