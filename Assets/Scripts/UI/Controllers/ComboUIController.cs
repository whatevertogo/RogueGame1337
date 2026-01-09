using System;
using Core.Events;
using Cysharp.Threading.Tasks;
using Game.UI;
using RogueGame.Events;

namespace UI
{
    /// <summary>
    /// 连击 UI 控制器（纯 C#，无 Mono）
    /// 职责：
    /// - 游戏启动时打开 ComboUI（常驻显示）
    /// - 订阅游戏流程事件，自动显示/隐藏 ComboUI
    /// </summary>
    public static class ComboUIController
    {
        private static bool _initialized;
        private static UIManager _uiManager;

        /// <summary>
        /// 初始化 ComboUI 控制器（在 GameRoot 中调用）
        /// </summary>
        public static void Initialize(UIManager uiManager)
        {
            if (_initialized)
                return;
            _initialized = true;
            _uiManager = uiManager;

            // 订阅游戏流程事件，在合适的时机打开/关闭 ComboUI
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<RoomClearedEvent>(OnRoomCleared);
            EventBus.Subscribe<LayerTransitionEvent>(OnLayerTransition);

            // 游戏启动后打开 ComboUI（初始隐藏，由 ComboUIViewLogic 控制显示逻辑）
            OpenComboUIAsync().Forget();
        }

        /// <summary>
        /// 异步打开 ComboUI
        /// </summary>
        private static async UniTaskVoid OpenComboUIAsync()
        {
            try
            {
                await _uiManager.Open<ComboUIView>();
                UnityEngine.Debug.Log("[ComboUIController] ComboUI 已打开");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ComboUIController] 打开 ComboUI 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 战斗开始时不需要特殊处理（ComboUI 已常驻）
        /// </summary>
        private static void OnCombatStarted(CombatStartedEvent evt)
        {
            // ComboUI 已常驻，无需操作
            // 连击数由 ComboManager 管理，UI 由 ComboUIViewLogic 驱动
        }

        /// <summary>
        /// 房间清除时不需要特殊处理（ComboUI 已常驻）
        /// </summary>
        private static void OnRoomCleared(RoomClearedEvent evt)
        {
            // ComboUI 已常驻，无需操作
        }

        /// <summary>
        /// 层过渡时不需要特殊处理（ComboUI 已常驻）
        /// </summary>
        private static void OnLayerTransition(LayerTransitionEvent evt)
        {
            // ComboUI 已常驻，无需操作
        }

        /// <summary>
        /// 关闭 ComboUI（游戏结束时调用）
        /// </summary>
        public static void Shutdown()
        {
            if (_uiManager == null)
                return;

            _uiManager.Close<ComboUIView>();
        }
    }
}
