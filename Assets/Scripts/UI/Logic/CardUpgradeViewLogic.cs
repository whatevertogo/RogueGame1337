using System;
using System.Collections.Generic;
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

        public virtual void Bind(UIViewBase view)
        {
            _view = view as CardUpgradeView;
            // 订阅进化请求事件
            EventBus.Subscribe<SkillEvolutionRequestedEvent>(OnEvolutionRequested);
            // 订阅进化完成事件（用于关闭UI）
            EventBus.Subscribe<SkillEvolvedEvent>(OnEvolutionCompleted);
        }

        public virtual void OnOpen(UIArgs args)
        {
            // UI 打开时的初始化逻辑
            // 实际显示内容由 OnEvolutionRequested 事件触发
        }

        public virtual void OnClose()
        {
            // 关闭时清理
            EventBus.Unsubscribe<SkillEvolutionRequestedEvent>(OnEvolutionRequested);
            EventBus.Unsubscribe<SkillEvolvedEvent>(OnEvolutionCompleted);
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
        /// 处理技能进化请求事件
        /// </summary>
        private void OnEvolutionRequested(SkillEvolutionRequestedEvent evt)
        {
            // 存储当前上下文
            _currentInstanceId = evt.InstanceId;
            _currentCardId = evt.CardId;
            _currentLevel = evt.CurrentLevel;
            _nextLevel = evt.NextLevel;

            // 获取分支信息
            var upgradeService = ServiceLocator.Get<ActiveCardUpgradeService>();
            if (upgradeService != null)
            {
                (_branchA, _branchB) = upgradeService.GetEvolutionBranches(evt.InstanceId);
            }

            // 更新 UI 显示
            UpdateDisplay();
        }

        /// <summary>
        /// 处理技能进化完成事件
        /// </summary>
        private void OnEvolutionCompleted(SkillEvolvedEvent evt)
        {
            // 进化完成后关闭 UI
            if (evt.InstanceId == _currentInstanceId)
            {
                CloseUI();
            }
        }

        /// <summary>
        /// 更新 UI 显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (_view == null) return;

            // 显示技能名称和等级
            var skillDef = GameRoot.Instance?.CardDatabase?.Resolve(_currentCardId).activeCardConfig.skill;
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
            if (string.IsNullOrEmpty(_currentInstanceId)) return;

            var upgradeService = ServiceLocator.Get<ActiveCardUpgradeService>();
            if (upgradeService != null)
            {
                upgradeService.ConfirmEvolution(_currentInstanceId, chooseBranchA: true);
            }
        }

        /// <summary>
        /// 选择分支 B
        /// </summary>
        public void OnOption2ImageClicked()
        {
            if (string.IsNullOrEmpty(_currentInstanceId)) return;

            var upgradeService = ServiceLocator.Get<ActiveCardUpgradeService>();
            if (upgradeService != null)
            {
                upgradeService.ConfirmEvolution(_currentInstanceId, chooseBranchA: false);
            }
        }

        /// <summary>
        /// 关闭 UI
        /// </summary>
        private void CloseUI()
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
        }

        /// <summary>
        /// 显示升级消息（保留原有的升级提示功能）
        /// </summary>
        public void ShowLevelUpMessage(ActiveCardLevelUpEvent evt)
        {
            _view?.SetSkillNameText(evt.CardId);
            _view?.SetSkillLevelText($"已升级至 Lv{evt.NewLevel}");
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// </summary>
    public class CardUpgradeViewLogic : MonoBehaviour, IUILogic
    {
        private CardUpgradeViewLogicCore _core = new CardUpgradeViewLogicCore();
        private CardUpgradeView _view;

        public void Bind(UIViewBase view)
        {
            _view = view as CardUpgradeView;
            if (_view == null)
            {
                Debug.LogError($"[UI] CardUpgradeViewLogic: Bind failed! View is not {typeof(CardUpgradeView)}");
                return;
            }

            _core.Bind(view);
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
