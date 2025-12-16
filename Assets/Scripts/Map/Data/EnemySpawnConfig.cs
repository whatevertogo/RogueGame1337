using System. Collections.Generic;
using UnityEngine;

namespace RogueGame.Map
{
    /// <summary>
    /// 敌人生成条目
    /// </summary>
    [System.Serializable]
    public sealed class EnemySpawnEntry
    {
        public GameObject prefab;
        
        [Range(1, 100)]
        public int weight = 50;
        
        [Tooltip("最早出现的层数")]
        public int minFloor = 1;
        
        [Tooltip("最晚出现的层数（0=无限制）")]
        public int maxFloor = 0;
    }

    /// <summary>
    /// 敌人生成配置
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "RogueGame/Room/Enemy Spawn Config")]
    public class EnemySpawnConfig :  ScriptableObject
    {
        [Header("普通敌人")]
        public List<EnemySpawnEntry> normalEnemies = new();

        [Header("精英敌人")]
        public List<EnemySpawnEntry> eliteEnemies = new();

        [Header("Boss")]
        public List<EnemySpawnEntry> bosses = new();

        /// <summary>
        /// 根据层数和权重选择敌人
        /// </summary>
        public GameObject SelectEnemy(List<EnemySpawnEntry> pool, int currentFloor)
        {
            if (pool == null || pool.Count == 0) return null;

            // 过滤可用敌人
            var available = new List<EnemySpawnEntry>();
            foreach (var entry in pool)
            {
                if (entry.prefab == null) continue;
                if (currentFloor < entry.minFloor) continue;
                if (entry.maxFloor > 0 && currentFloor > entry. maxFloor) continue;
                
                available.Add(entry);
            }

            if (available. Count == 0) return null;

            // 权重选择
            int totalWeight = 0;
            foreach (var entry in available)
            {
                totalWeight += entry.weight;
            }

            int random = Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var entry in available)
            {
                cumulative += entry.weight;
                if (random < cumulative)
                {
                    return entry.prefab;
                }
            }

            return available[0].prefab;
        }
    }
}