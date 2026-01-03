using System;
using Character.Player.Skill.Evolution;
using Core.Events;
using RogueGame.Events;
using RogueGame.Game.Service.Inventory;
using UI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// CardUpgradeView 纯逻辑核心（可单元测试）
    /// </summary>
    public class CardUpgradeViewLogicCore
    {
        protected CardUpgradeView _view;

        // 当前进化请求的上下文
        private string _currentInstanceId;
        private string _currentCardId;
        private int _currentLevel;
        private int _nextLevel;
        private SkillBranch _branchA;
        private SkillBranch _branchB;
        private Character.Player.Skill.Evolution.SkillNode _evolutionNode;

        public virtual void Bind(UIViewBase view)
        {
            _view = view as CardUpgradeView;
        }

        public virtual void OnOpen(UIArgs args)
        {
            // UI 打开时的初始化逻辑
            // 显示内容由 ProcessEvolutionRequest 设置
        }

        public virtual void OnClose()
        {
            ClearCurrentContext();
            _view?.ClearDisplay();
        }

        public virtual void OnCovered()
        {
            // 被同层新 UI 覆盖时的默认处理（子类可重写）
        }

        public virtual void OnResume()
        {
            // 从覆盖状态恢复时的默认处理（子类可重写）
        }

        /// <summary>
        /// 处理进化请求（由 MonoBehaviour 层调用）
        /// </summary>
        public void ProcessEvolutionRequest(SkillEvolutionRequestedEvent evt)
        {
            // 存储当前上下文
            _currentInstanceId = evt.InstanceId;
            _currentCardId = evt.CardId;
            _currentLevel = evt.CurrentLevel;
            _nextLevel = evt.NextLevel;
            _evolutionNode = evt.EvolutionNode;

            // 获取分支信息
            var upgradeService = ServiceLocator.Get<ActiveCardUpgradeService>();
            if (upgradeService == null)
            {
                Debug.LogError("[CardUpgradeViewLogicCore] 无法获取 ActiveCardUpgradeService，请检查 GameRoot 初始化");
                Debug.Log(ServiceLocator.GetDebugInfo());
                return;
            }

            (_branchA, _branchB) = upgradeService.GetEvolutionBranches(evt.InstanceId);

            // 更新 UI 显示
            UpdateDisplay();
        }

        /// <summary>
        /// 更新 UI 显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (_view == null) return;

            // 显示技能名称和等级
            var skillDef = GameRoot.Instance?.CardDatabase?.Resolve(_currentCardId)?.activeCardConfig?.skill;
            string skillName = skillDef?.skillId ?? _currentCardId;
            _view.SetSkillNameText(skillName);
            _view.SetSkillLevelText($"Lv{_currentLevel} → Lv{_nextLevel}");

            // 显示分支 A 信息
            if (_branchA != null)
            {
                _view.SetOption1(_branchA.branchName, _branchA.description, _branchA.icon);
            }
            else
            {
                _view.SetOption1("分支 A", "暂无描述", null);
            }

            // 显示分支 B 信息
            if (_branchB != null)
            {
                _view.SetOption2(_branchB.branchName, _branchB.description, _branchB.icon);
            }
            else
            {
                _view.SetOption2("分支 B", "暂无描述", null);
            }
        }

        /// <summary>
        /// 选择分支 A
        /// </summary>
        public void OnOption1ImageClicked()
        {
            if (string.IsNullOrEmpty(_currentInstanceId))
            {
                Debug.LogWarning("[CardUpgradeViewLogicCore] OnOption1ImageClicked: _currentInstanceId 为空");
                return;
            }

            var upgradeService = ServiceLocator.Get<ActiveCardUpgradeService>();
            if (upgradeService == null)
            {
                Debug.LogError("[CardUpgradeViewLogicCore] OnOption1ImageClicked: 无法获取 ActiveCardUpgradeService");
                Debug.Log(ServiceLocator.GetDebugInfo());
                return;
            }

            upgradeService.ConfirmEvolution(_currentInstanceId, chooseBranchA: true);
        }

        /// <summary>
        /// 选择分支 B
        /// </summary>
        public void OnOption2ImageClicked()
        {
            if (string.IsNullOrEmpty(_currentInstanceId))
            {
                Debug.LogWarning("[CardUpgradeViewLogicCore] OnOption2ImageClicked: _currentInstanceId 为空");
                return;
            }

            var upgradeService = ServiceLocator.Get<ActiveCardUpgradeService>();
            if (upgradeService == null)
            {
                Debug.LogError("[CardUpgradeViewLogicCore] OnOption2ImageClicked: 无法获取 ActiveCardUpgradeService");
                Debug.Log(ServiceLocator.GetDebugInfo());
                return;
            }

            upgradeService.ConfirmEvolution(_currentInstanceId, chooseBranchA: false);
        }

        /// <summary>
        /// 关闭 UI
        /// </summary>
        public void CloseUI()
        {
            _view?.Close();
            ClearCurrentContext();
        }

        /// <summary>
        /// 清空当前上下文
        /// </summary>
        private void ClearCurrentContext()
        {
            _currentInstanceId = null;
            _currentCardId = null;
            _currentLevel = 0;
            _nextLevel = 0;
            _branchA = null;
            _branchB = null;
            _evolutionNode = null;
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// 同时负责订阅事件并打开 UI
    /// </summary>
    public class CardUpgradeViewLogic : MonoBehaviour, IUILogic
    {
        private CardUpgradeViewLogicCore _core = new CardUpgradeViewLogicCore();
        private CardUpgradeView _view;

        /// <summary>
        /// 当前待处理的进化请求（用于 UI 打开后传递给 Core）
        /// </summary>
        private static SkillEvolutionRequestedEvent _pendingEvent;
        private bool _subscribed = false;

        private void OnEnable()
        {
            // 在 OnEnable 中订阅事件，此时 GameRoot 已初始化完成
            if (!_subscribed)
            {
                EventBus.Subscribe<SkillEvolutionRequestedEvent>(OnEvolutionRequested);
                EventBus.Subscribe<SkillEvolvedEvent>(OnEvolutionCompleted);
                _subscribed = true;
                Debug.Log("[CardUpgradeViewLogic] 已订阅进化事件");
            }
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<SkillEvolutionRequestedEvent>(OnEvolutionRequested);
                EventBus.Unsubscribe<SkillEvolvedEvent>(OnEvolutionCompleted);
                _subscribed = false;
                Debug.Log("[CardUpgradeViewLogic] 已取消订阅进化事件");
            }
        }

        private void OnDestroy()
        {
            // 保险措施：确保取消订阅
            OnDisable();
        }

        /// <summary>
        /// 处理技能进化请求事件（全局订阅）
        /// </summary>
        private void OnEvolutionRequested(SkillEvolutionRequestedEvent evt)
        {
            // 保存事件，等 UI 打开后处理
            _pendingEvent = evt;

            // 打开 UI（如果还未打开）
            if (_view == null || !_view.gameObject.activeInHierarchy)
            {
                UIManager.Instance?.Open<CardUpgradeView>();
            }
            else
            {
                // UI 已打开，直接处理
                _core?.ProcessEvolutionRequest(evt);
            }
        }

        /// <summary>
        /// 处理技能进化完成事件
        /// </summary>
        private void OnEvolutionCompleted(SkillEvolvedEvent evt)
        {
            // 进化完成后关闭 UI
            if (_pendingEvent != null && evt.InstanceId == _pendingEvent.InstanceId)
            {
                _core?.CloseUI();
                _pendingEvent = null;
            }
        }

        public void Bind(UIViewBase view)
        {
            _view = view as CardUpgradeView;
            if (_view == null)
            {
                Debug.LogError($"[UI] CardUpgradeViewLogic: Bind failed! View is not {typeof(CardUpgradeView)}");
                return;
            }

            _core.Bind(view);

            // 如果有待处理的事件，现在处理它
            if (_pendingEvent != null)
            {
                _core.ProcessEvolutionRequest(_pendingEvent);
            }

            // Auto-bind event for option1Image
            _view.BindOption1ImageButton(OnOption1ImageClicked);
            // Auto-bind event for option2Image
            _view.BindOption2ImageButton(OnOption2ImageClicked);
        }

        public void OnOpen(UIArgs args)
        {
            _core.OnOpen(args);
        }

        public void OnClose()
        {
            _core.OnClose();
            if (_view != null)
            {
                _view.BindOption1ImageButton(null);
                _view.BindOption2ImageButton(null);
            }
            _view = null;
            _pendingEvent = null;
        }

        public void OnCovered()
        {
            _core.OnCovered();
        }

        public void OnResume()
        {
            _core.OnResume();
        }

        private void OnOption1ImageClicked()
        {
            _core.OnOption1ImageClicked();
        }

        private void OnOption2ImageClicked()
        {
            _core.OnOption2ImageClicked();
        }
    }
}
