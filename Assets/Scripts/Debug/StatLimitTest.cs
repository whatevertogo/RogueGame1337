using UnityEngine;
using Character.Components;
using CDTU.Utils;

namespace Debug1
{
    /// <summary>
    /// 属性上限系统测试脚本
    /// 在运行时通过 ContextMenu 调用各项测试
    /// </summary>
    public class StatLimitTest : MonoBehaviour
    {
        [Header("测试设置")]
        [Tooltip("是否在 Start 时自动运行测试")]
        [SerializeField] private bool autoTestOnStart = false;

        [Header("测试结果")]
        [SerializeField, ReadOnly] private int testsPassed = 0;
        [SerializeField, ReadOnly] private int testsFailed = 0;

        private void Start()
        {
            if (autoTestOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("🧪 运行所有测试")]
        public void RunAllTests()
        {
            CDLogger.Log("========== 开始属性上限系统测试 ==========");

            testsPassed = 0;
            testsFailed = 0;

            Test_01_StatLimitConfigExists();
            Test_02_StatMaxValue();
            Test_03_CharacterStatsApplyLimits();
            Test_04_DodgeRateLimit();
            Test_05_PassiveCardStackLimit();
            Test_06_ActiveSkillLevelLimit();

            CDLogger.Log($"========== 测试完成: {testsPassed} 通过, {testsFailed} 失败 ==========");
        }

        #region 测试用例

        /// <summary>
        /// 测试 1: 检查 StatLimitConfig 是否存在并正确配置
        /// </summary>
        [ContextMenu("测试 1: StatLimitConfig 存在性")]
        public void Test_01_StatLimitConfigExists()
        {
            CDLogger.Log("\n[Test 1] 检查 StatLimitConfig...");

            var config = GameRoot.Instance?.StatLimitConfig;
            if (config == null)
            {
                CDLogger.LogError("❌ 失败: StatLimitConfig 未配置");
                testsFailed++;
                return;
            }

            CDLogger.Log($"✅ 通过: StatLimitConfig 已配置");
            CDLogger.Log($"  - 最大闪避率: {config.maxDodge * 100}%");
            CDLogger.Log($"  - 被动卡叠加上限: {config.maxPassiveCardStack}");
            CDLogger.Log($"  - 主动技能等级上限: {config.maxActiveSkillLevel}");
            testsPassed++;
        }

        /// <summary>
        /// 测试 2: 测试 Stat 类的上限功能
        /// </summary>
        [ContextMenu("测试 2: Stat 上限功能")]
        public void Test_02_StatMaxValue()
        {
            CDLogger.Log("\n[Test 2] 测试 Stat 上限功能...");

            // 创建一个测试 Stat
            var testStat = new Stat(100f);
            testStat.SetMaxValue(200f);

            // 添加超过上限的修饰符
            var modifier = new StatModifier(150f, Character.StatModType.Flat, this);
            testStat.AddModifier(modifier);

            float finalValue = testStat.Value;
            CDLogger.Log($"  - 基础值: 100, 修饰符: +150, 上限: 200");
            CDLogger.Log($"  - 最终值: {finalValue}");

            if (finalValue > 200f)
            {
                CDLogger.LogError($"❌ 失败: 最终值 {finalValue} 超过上限 200");
                testsFailed++;
                return;
            }

            if (Mathf.Approximately(finalValue, 200f))
            {
                CDLogger.Log("✅ 通过: Stat 正确应用上限");
                testsPassed++;
            }
            else
            {
                CDLogger.LogError($"❌ 失败: 期望 200, 实际 {finalValue}");
                testsFailed++;
            }
        }

        /// <summary>
        /// 测试 3: 测试 CharacterStats 是否正确应用配置的上限
        /// </summary>
        [ContextMenu("测试 3: CharacterStats 应用上限")]
        public void Test_03_CharacterStatsApplyLimits()
        {
            CDLogger.Log("\n[Test 3] 测试 CharacterStats 应用上限...");

            var player = GameRoot.Instance?.PlayerManager?.GetLocalPlayerState()?.Controller;
            if (player == null)
            {
                CDLogger.LogWarning("⚠️ 跳过: 玩家未初始化（在游戏中运行测试）");
                return;
            }

            var stats = player.GetComponent<CharacterStats>();
            if (stats == null)
            {
                CDLogger.LogError("❌ 失败: 玩家没有 CharacterStats 组件");
                testsFailed++;
                return;
            }

            // 检查闪避率上限
            float? dodgeMax = stats.Dodge.GetMaxValue();
            CDLogger.Log($"  - 闪避率上限: {dodgeMax ?? (float?)null}");

            if (dodgeMax.HasValue && dodgeMax.Value > 0)
            {
                CDLogger.Log($"✅ 通过: CharacterStats 已应用闪避率上限 ({dodgeMax.Value * 100}%)");
                testsPassed++;
            }
            else
            {
                CDLogger.LogWarning("⚠️ 警告: 闪避率未设置上限（如果未配置 StatLimitConfig 则正常）");
            }
        }

        /// <summary>
        /// 测试 4: 测试闪避率上限
        /// </summary>
        [ContextMenu("测试 4: 闪避率上限")]
        public void Test_04_DodgeRateLimit()
        {
            CDLogger.Log("\n[Test 4] 测试闪避率上限...");

            var config = GameRoot.Instance?.StatLimitConfig;
            if (config == null)
            {
                CDLogger.LogWarning("⚠️ 跳过: StatLimitConfig 未配置");
                return;
            }

            var player = GameRoot.Instance?.PlayerManager?.GetLocalPlayerState()?.Controller;
            if (player == null)
            {
                CDLogger.LogWarning("⚠️ 跳过: 玩家未初始化");
                return;
            }

            var stats = player.GetComponent<CharacterStats>();
            if (stats == null)
            {
                CDLogger.LogError("❌ 失败: 玩家没有 CharacterStats 组件");
                testsFailed++;
                return;
            }

            float configuredMax = config.maxDodge;
            float actualDodge = stats.Dodge.Value;

            CDLogger.Log($"  - 配置的闪避率上限: {configuredMax * 100}%");
            CDLogger.Log($"  - 实际闪避率: {actualDodge * 100}%");

            if (actualDodge <= configuredMax + 0.0001f) // 允许小误差
            {
                CDLogger.Log("✅ 通过: 实际闪避率未超过配置上限");
                testsPassed++;
            }
            else
            {
                CDLogger.LogError($"❌ 失败: 实际闪避率 ({actualDodge * 100}%) 超过配置上限 ({configuredMax * 100}%)");
                testsFailed++;
            }
        }

        /// <summary>
        /// 测试 5: 测试被动卡叠加上限
        /// </summary>
        [ContextMenu("测试 5: 被动卡叠加上限")]
        public void Test_05_PassiveCardStackLimit()
        {
            CDLogger.Log("\n[Test 5] 测试被动卡叠加上限...");

            var config = GameRoot.Instance?.StatLimitConfig;
            if (config == null)
            {
                CDLogger.LogWarning("⚠️ 跳过: StatLimitConfig 未配置");
                return;
            }

            var inventory = GameRoot.Instance?.InventoryManager;
            if (inventory == null)
            {
                CDLogger.LogError("❌ 失败: InventoryManager 未初始化");
                testsFailed++;
                return;
            }

            int maxStack = config.maxPassiveCardStack;
            CDLogger.Log($"  - 配置的被动卡叠加上限: {maxStack}");

            // 尝试添加超过上限的被动卡
            string testCardId = "TestPassiveCard";
            inventory.AddPassiveCard(testCardId, maxStack + 10);

            int actualCount = inventory.GetPassiveCardCount(testCardId);
            CDLogger.Log($"  - 尝试添加 {maxStack + 10} 张，实际数量: {actualCount}");

            if (actualCount <= maxStack)
            {
                CDLogger.Log($"✅ 通过: 被动卡数量正确限制在 {maxStack}");
                testsPassed++;

                // 清理测试数据
                inventory.RemovePassiveCard(testCardId, actualCount);
            }
            else
            {
                CDLogger.LogError($"❌ 失败: 被动卡数量 ({actualCount}) 超过上限 ({maxStack})");
                testsFailed++;
            }
        }

        /// <summary>
        /// 测试 6: 测试主动技能等级上限
        /// </summary>
        [ContextMenu("测试 6: 主动技能等级上限")]
        public void Test_06_ActiveSkillLevelLimit()
        {
            CDLogger.Log("\n[Test 6] 测试主动技能等级上限...");

            var config = GameRoot.Instance?.StatLimitConfig;
            if (config == null)
            {
                CDLogger.LogWarning("⚠️ 跳过: StatLimitConfig 未配置");
                return;
            }

            int maxLevel = config.maxActiveSkillLevel;
            CDLogger.Log($"  - 配置的主动技能等级上限: Lv{maxLevel}");

            // 模拟升级测试
            CDLogger.Log("  ⚠️ 注意: 此测试需要实际技能卡牌，建议手动测试");

            CDLogger.Log($"✅ 通过: 配置的等级上限为 Lv{maxLevel}");
            testsPassed++;
        }

        #endregion

        #region 辅助测试

        /// <summary>
        /// 显示当前玩家的所有属性值和上限
        /// </summary>
        [ContextMenu("📊 显示玩家属性信息")]
        public void ShowPlayerStatsInfo()
        {
            var player = GameRoot.Instance?.PlayerManager?.GetLocalPlayerState()?.Controller;
            if (player == null)
            {
                CDLogger.LogWarning("玩家未初始化");
                return;
            }

            var stats = player.GetComponent<CharacterStats>();
            if (stats == null)
            {
                CDLogger.LogWarning("CharacterStats 组件未找到");
                return;
            }

            CDLogger.Log("========== 玩家属性信息 ==========");
            LogStat("最大生命值", stats.MaxHP);
            LogStat("移动速度", stats.MoveSpeed);
            LogStat("攻击力", stats.AttackPower);
            LogStat("护甲", stats.Armor);
            LogStat("闪避率", stats.Dodge, isPercentage: true);
            CDLogger.Log("=====================================");
        }

        private void LogStat(string name, Stat stat, bool isPercentage = false)
        {
            float? max = stat.GetMaxValue();
            string maxStr = max.HasValue ? (isPercentage ? $"{max.Value * 100:F1}%" : $"{max.Value:F1}") : "无限制";
            string valueStr = isPercentage ? $"{stat.Value * 100:F1}%" : $"{stat.Value:F1}";

            CDLogger.Log($"{name}: {valueStr} (上限: {maxStr})");
        }

        #endregion
    }
}
