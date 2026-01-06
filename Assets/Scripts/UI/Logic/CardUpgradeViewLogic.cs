using System;
using System.Collections.Generic;
using Character.Player.Skill.Evolution;
using Core.Events;
using RogueGame.Events;
using RogueGame.Game.Service;
using UnityEngine;
using UI;

namespace Game.UI
{
    /// <summary>
    /// CardUpgradeView 纯逻辑核心（可单元测试）
    /// 支持效果池系统的动态选项
    /// </summary>
    public class CardUpgradeViewLogicCore
    {
        protected CardUpgradeView _view;
        private SkillEvolutionRequestedEvent _currentEvolution;

        /// <summary>
        /// 当前可用的进化效果列表（效果池系统）
        /// </summary>
        private List<EvolutionEffectEntry> _availableOptions;

        public virtual void Bind(UIViewBase view)
        {
            _view = view as CardUpgradeView;
        }

        public virtual void OnOpen(UIArgs args)
        {
            var evolutionArgs = args as SkillEvolutionUIArgs;
            if (evolutionArgs == null || evolutionArgs.Event == null) return;

            _currentEvolution = evolutionArgs.Event;
            var evt = evolutionArgs.Event;

            _view.SetSkillNameText(evt.CardId);
            _view.SetSkillLevelText($"等级 {evt.NextLevel}");

            // 从效果池设置选项
            if (evt.Options != null && evt.Options.Count > 0)
            {
                _availableOptions = evt.Options;
                SetupOptionsFromPool(evt.Options);
            }
        }

        /// <summary>
        /// 从效果池设置选项
        /// </summary>
        private void SetupOptionsFromPool(List<EvolutionEffectEntry> options)
        {
            // 当前 UI 只有2个固定选项槽，最多显示前2个
            // TODO: 后续可扩展为支持动态数量的选项槽（3-4个）

            for (int i = 0; i < options.Count && i < 2; i++)
            {
                var effect = options[i];
                string name = effect.effectName ?? effect.effectId;
                string desc = effect.description ?? "无描述";
                Sprite icon = effect.icon;

                // 使用稀有度颜色
                Color rarityColor = effect.GetRarityColor();
                string rarityName = effect.GetRarityDisplayName();

                // 显示稀有度前缀
                string displayName = $"[{rarityName}] {name}";

                if (i == 0)
                {
                    _view.SetOption1(displayName, desc, icon, rarityColor);
                }
                else if (i == 1)
                {
                    _view.SetOption2(displayName, desc, icon, rarityColor);
                }
            }

            // 隐藏未使用的选项槽
            if (options.Count < 1) _view.HideOption1();
            if (options.Count < 2) _view.HideOption2();

            Debug.Log($"[CardUpgradeViewLogic] 显示 {options.Count} 个进化选项");
        }

        public virtual void OnClose()
        {
            _currentEvolution = null;
            _availableOptions = null;
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
            ConfirmEvolution(0);
        }

        public void OnOption2ImageClicked()
        {
            if (_currentEvolution == null) return;
            ConfirmEvolution(1);
        }

        /// <summary>
        /// 确认进化选择（效果池系统）
        /// </summary>
        /// <param name="optionIndex">选项索引（0-3）</param>
        private void ConfirmEvolution(int optionIndex)
        {
            if (_currentEvolution == null || _availableOptions == null) return;

            if (optionIndex >= _availableOptions.Count)
            {
                Debug.LogWarning($"[CardUpgradeViewLogic] 无效的选项索引: {optionIndex}");
                return;
            }

            var selectedEffect = _availableOptions[optionIndex];

            if (selectedEffect == null)
            {
                Debug.LogWarning("[CardUpgradeViewLogic] 选择的进化效果为空");
                return;
            }

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
                selectedEffect
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
            _view.BindOption1ImageButton(OnOption1ImageClicked);
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
