using System;
using Character.Player.Skill.Evolution;
using Core.Events;
using RogueGame.Events;
using RogueGame.Game.Service;
using RogueGame.Items;
using UnityEngine;
using UI;

namespace Game.UI
{
    /// <summary>
    /// CardUpgradeView 纯逻辑核心（可单元测试）
    /// </summary>
    public class CardUpgradeViewLogicCore
    {
        protected CardUpgradeView _view;
        private SkillEvolutionRequestedEvent _currentEvolution;

        public virtual void Bind(UIViewBase view)
        {
            _view = view as CardUpgradeView;
        }

        public virtual void OnOpen(UIArgs args)
        {
            var evolutionArgs = args as SkillEvolutionUIArgs;
            if (evolutionArgs == null || evolutionArgs.Event == null) return;

            // 保存进化请求信息
            _currentEvolution = evolutionArgs.Event;

            var evt = evolutionArgs.Event;
            var evolutionNode = evt.EvolutionNode;
            if (evolutionNode == null) return;

            _view.SetSkillNameText(evt.CardId);
            _view.SetSkillLevelText($"等级 {evt.NextLevel}");

            if (evolutionNode.branchA != null)
            {
                _view.SetOption1(evolutionNode.branchA.branchName,
                    evolutionNode.branchA.description,
                    evolutionNode.branchA.icon);
            }

            if (evolutionNode.branchB != null)
            {
                _view.SetOption2(evolutionNode.branchB.branchName,
                    evolutionNode.branchB.description,
                    evolutionNode.branchB.icon);
            }
        }

        public virtual void OnClose()
        {
            // 关闭时清理
            _currentEvolution = null;
            _view = null;
        }

        public virtual void OnCovered()
        {
            // 被同层新 UI 覆盖时的默认处理（子类可重写）
        }

        public virtual void OnResume()
        {
            // 从覆盖状态恢复时的默认处理（子类可重写）
        }

        public void OnOption1ImageClicked()
        {
            if (_currentEvolution == null) return;
            ConfirmEvolution(true);
        }

        public void OnOption2ImageClicked()
        {
            if (_currentEvolution == null) return;
            ConfirmEvolution(false);
        }

        /// <summary>
        /// 确认进化选择
        /// </summary>
        private void ConfirmEvolution(bool chooseBranchA)
        {
            if (_currentEvolution == null) return;

            var selectedBranch = chooseBranchA 
                ? _currentEvolution.EvolutionNode.branchA 
                : _currentEvolution.EvolutionNode.branchB;

            if (selectedBranch == null)
            {
                Debug.LogWarning("[CardUpgradeViewLogic] 选择的分支为空");
                return;
            }

            // 通过 InventoryManager 确认进化（职责分离）
            var inventoryManager = GameRoot.Instance?.InventoryManager;
            if (inventoryManager == null)
            {
                Debug.LogError("[CardUpgradeViewLogic] InventoryManager 为空");
                return;
            }

            bool success = inventoryManager.ConfirmEvolution(
                _currentEvolution.InstanceId,
                _currentEvolution.CardId,
                _currentEvolution.CurrentLevel,
                _currentEvolution.NextLevel,
                chooseBranchA,
                selectedBranch
            );

            if (!success)
            {
                Debug.LogError($"[CardUpgradeViewLogic] 进化确认失败: {_currentEvolution.InstanceId}");
            }
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// 继承 UILogicBase 以获得自动事件订阅管理能力
    /// </summary>
    public class CardUpgradeViewLogic : UILogicBase
    {
        private CardUpgradeViewLogicCore _core = new CardUpgradeViewLogicCore();
        private CardUpgradeView _view;

        public override void Bind(UIViewBase view)
        {
            base.Bind(view);
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

        public override void OnOpen(UIArgs args)
        {
            base.OnOpen(args);
            _core.OnOpen(args);
        }

        public override void OnClose()
        {
            _core.OnClose();
            // Button 事件由 UIViewBase.BindButton 自动清理，无需手动解绑
            _view = null;
            base.OnClose();
        }

        public override void OnCovered()
        {
            base.OnCovered();
            _core.OnCovered();
        }

        public override void OnResume()
        {
            base.OnResume();
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
