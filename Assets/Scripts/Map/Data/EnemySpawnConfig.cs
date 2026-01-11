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

        [Tooltip("该敌人的最佳层数（用于动态权重调整，0=使用maxFloor或minFloor+2）")]
        public int optimalFloor = 0;

        [Tooltip("权重调整敏感度（值越高，层数偏离时权重下降越快）")]
        [Range(0.1f, 2f)]
        public float weightDecayFactor = 0.5f;
    }

    /// <summary>
    /// 敌人生成配置
    /// </summary>
    [ManagedData("Map")]
    [CreateAssetMenu(fileName = "EnemySpawn", menuName = "RogueGame/Map/Enemy Spawn Config")]
    public class EnemySpawnConfig :  ScriptableObject
    {
        [Header("权重调整设置")]
        [Tooltip("启用基于层数的动态权重调整")]
        public bool enableDynamicWeighting = true;

        [Tooltip("权重调整算法")]
        public WeightAdjustmentType weightAdjustmentType = WeightAdjustmentType.Linear;

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
                if (entry.maxFloor > 0 && currentFloor > entry.maxFloor) continue;

                available.Add(entry);
            }

            if (available.Count == 0) return null;

            // 权重选择（启用动态权重调整时）
            int totalWeight = 0;
            var adjustedWeights = new int[available.Count];

            for (int i = 0; i < available.Count; i++)
            {
                int weight = available[i].weight;

                // 应用基于层数的权重调整
                if (enableDynamicWeighting)
                {
                    weight = CalculateAdjustedWeight(available[i], currentFloor);
                }

                adjustedWeights[i] = weight;
                totalWeight += weight;
            }

            int random = Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < available.Count; i++)
            {
                cumulative += adjustedWeights[i];
                if (random < cumulative)
                {
                    return available[i].prefab;
                }
            }

            return available[0].prefab;
        }

        /// <summary>
        /// 计算基于层数调整后的权重
        /// </summary>
        /// <param name="entry">敌人生成条目</param>
        /// <param name="currentFloor">当前层数</param>
        /// <returns>调整后的权重</returns>
        private int CalculateAdjustedWeight(EnemySpawnEntry entry, int currentFloor)
        {
            // 确定最佳层数
            int optimal = entry.optimalFloor > 0 ? entry.optimalFloor : GetDefaultOptimalFloor(entry);

            // 计算层数差距
            int floorDiff = Mathf.Abs(currentFloor - optimal);

            // 如果层数差距为0，使用原始权重
            if (floorDiff == 0)
            {
                return entry.weight;
            }

            // 根据调整类型计算衰减后的权重
            float multiplier = CalculateWeightMultiplier(floorDiff, entry.weightDecayFactor);

            // 确保权重至少为1（避免完全不被选中）
            int adjustedWeight = Mathf.RoundToInt(entry.weight * multiplier);
            return Mathf.Max(1, adjustedWeight);
        }

        /// <summary>
        /// 获取默认的最佳层数
        /// </summary>
        private int GetDefaultOptimalFloor(EnemySpawnEntry entry)
        {
            // 如果有maxFloor限制，使用maxFloor
            if (entry.maxFloor > 0)
            {
                return entry.maxFloor;
            }

            // 否则使用minFloor + 2（假设敌人设计为在minFloor后2层达到最佳状态）
            return entry.minFloor + 2;
        }

        /// <summary>
        /// 计算权重乘数（基于层数差距和衰减因子）
        /// </summary>
        /// <param name="floorDiff">层数差距</param>
        /// <param name="decayFactor">衰减因子</param>
        /// <returns>权重乘数（0-1之间）</returns>
        private float CalculateWeightMultiplier(int floorDiff, float decayFactor)
        {
            return weightAdjustmentType switch
            {
                WeightAdjustmentType.Linear => LinearDecay(floorDiff, decayFactor),
                WeightAdjustmentType.Exponential => ExponentialDecay(floorDiff, decayFactor),
                WeightAdjustmentType.Sigmoid => SigmoidDecay(floorDiff, decayFactor),
                _ => 1f
            };
        }

        /// <summary>
        /// 线性衰减：每层差距减少一定比例的权重
        /// </summary>
        private float LinearDecay(int floorDiff, float decayFactor)
        {
            float multiplier = 1f - (floorDiff * decayFactor * 0.2f);
            return Mathf.Max(0.1f, multiplier); // 最低保留10%权重
        }

        /// <summary>
        /// 指数衰减：层数差距越大，权重下降越快
        /// </summary>
        private float ExponentialDecay(int floorDiff, float decayFactor)
        {
            float multiplier = Mathf.Exp(-floorDiff * decayFactor);
            return Mathf.Max(0.05f, multiplier); // 最低保留5%权重
        }

        /// <summary>
        /// Sigmoid衰减：平滑的S曲线
        /// </summary>
        private float SigmoidDecay(int floorDiff, float decayFactor)
        {
            // 使用sigmoid函数的变体，在floorDiff=0时为1，随floorDiff增加平滑下降
            float x = floorDiff * decayFactor;
            float multiplier = 1f / (1f + Mathf.Exp(x - 2f));
            return Mathf.Max(0.1f, multiplier);
        }
    }

    /// <summary>
    /// 权重调整类型
    /// </summary>
    public enum WeightAdjustmentType
    {
        /// <summary>线性衰减（每层固定减少）</summary>
        Linear,

        /// <summary>指数衰减（差距越大下降越快）</summary>
        Exponential,

        /// <summary>Sigmoid衰减（平滑S曲线）</summary>
        Sigmoid
    }
}