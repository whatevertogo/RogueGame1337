using System;
using Core.Events;
using Cysharp.Threading.Tasks;
using Game.UI;
using RogueGame.Events;

namespace UI
{
    /// <summary>
    /// 技能进化 UI 控制器（纯 C#，无 Mono）
    /// - 启动时订阅进化相关事件
    /// - 收到请求时打开 UI，并携带参数
    /// - 进化完成时关闭 UI
    /// </summary>
    public static class SkillEvolutionUIController
    {
        private static bool _initialized;
        private static UIManager _uiManager;
        private static SkillEvolutionRequestedEvent _pending;

        public static void Initialize(UIManager uiManager)
        {
            if (_initialized) return;
            _initialized = true;
            _uiManager = uiManager;

            EventBus.Subscribe<SkillEvolutionRequestedEvent>(OnEvolutionRequested);
            EventBus.Subscribe<SkillEvolvedEvent>(OnEvolutionCompleted);
        }

        private static async UniTaskVoid OnEvolutionRequested(SkillEvolutionRequestedEvent evt)
        {
            GameRoot.I?.GameFlowCoordinator?.PauseGame();
            _pending = evt;
            if (_uiManager == null)
            {
                UnityEngine.Debug.LogError("[SkillEvolutionUIController] UIManager is null, cannot open UI.");
                GameRoot.I?.GameFlowCoordinator?.ResumeGame();
                return;
            }

            try
            {
                await _uiManager.Open<CardUpgradeView>(new SkillEvolutionUIArgs(evt));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                GameRoot.I?.GameFlowCoordinator?.ResumeGame();
            }
        }

        private static void OnEvolutionCompleted(SkillEvolvedEvent evt)
        {
            if (_pending != null && evt.InstanceId == _pending.InstanceId)
            {
                _pending = null;
            }

            if (_uiManager == null) return;
            _uiManager.Close<CardUpgradeView>();
            GameRoot.I?.GameFlowCoordinator?.ResumeGame();
        }
    }
}
