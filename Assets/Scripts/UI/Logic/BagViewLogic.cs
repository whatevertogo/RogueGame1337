using UnityEngine;
using UI;
using Character.Components;
using Character.Player;
using RogueGame.Items;
using Core.Events;

namespace Game.UI
{
    /// <summary>
    /// BagView 纯逻辑核心（可单元测试）
    /// </summary>
    public class BagViewLogicCore
    {
        protected BagViewView _view;

        private CharacterStats localCharacterStats;
        private PlayerController localplayerController;


        public virtual void Bind(UIViewBase view)
        {
            _view = view as BagViewView;
            _view.BindPlayerStats1Button(OnPlayerStats1Clicked);
            _view.BindPlayerStats12Button(OnPlayerStats12Clicked);
            _view.BindClearCardButton1(OnClearCardButtonClicked);
        }

        public virtual void OnOpen(UIArgs args)
        {
            if (_view == null)
            {
                CDTU.Utils.CDLogger.LogError("[BagViewLogicCore] OnOpen() 中 _view 是 null！Bind() 可能没有被正确调用");
                return;
            }

            localplayerController = GameRoot.Instance.PlayerManager?.GetLocalPlayerRuntimeState()?.Controller;
            localCharacterStats = localplayerController?.GetComponent<CharacterStats>();
            if (localCharacterStats != null)
            {
                // 防止重复订阅：先移除再添加
                localCharacterStats.OnStatsChanged -= SetAllPlayerStatsText;
                localCharacterStats.OnStatsChanged += SetAllPlayerStatsText;

            }
            SetAllPlayerStatsText();
            //TODO- 设置玩家头像
            // _view.SetPlayerImage(localCharacterStats.Icon);


            // 初始化卡牌列表
            RefreshAllCardViews();

            //TODO=初始化装备槽
            // localPlayerSkillComponent.PlayerSkillSlots[1].Runtime.CardId
            // localPlayerSkillComponent.PlayerSkillSlots[2].Runtime.CardId
        }

        public virtual void OnClose()
        {
            // 关闭时清理
            if (localCharacterStats != null)
            {
                localCharacterStats.OnStatsChanged -= SetAllPlayerStatsText;
                localCharacterStats = null;
            }
            _view = null;
        }

        public void RefreshActiveCardViews()
        {
            var inv = InventoryServiceManager.Instance;
            if (inv == null)
            {
                CDTU.Utils.CDLogger.LogWarning("[BagView] InventoryManager.Instance is null");
                return;
            }

            if (_view == null)
            {
                CDTU.Utils.CDLogger.LogError("[BagView] _view 是 null！无法添加卡牌视图");
                return;
            }

            // 按 CardId 分组，找到最高等级的实例
            var cardGroups = new System.Collections.Generic.Dictionary<string, ActiveCardState>();

            foreach (var st in inv.ActiveCardStates)
            {
                if (st == null) continue;

                // 如果该 CardId 还没有记录，或者当前实例等级更高，则更新
                if (!cardGroups.ContainsKey(st.CardId) || st.Level > cardGroups[st.CardId].Level)
                {
                    cardGroups[st.CardId] = st;
                }
            }

            // 添加视图（每张卡只显示一次，显示最高等级）
            foreach (var kvp in cardGroups)
            {
                _view.AddCardView(kvp.Value.CardId, kvp.Value.Level);
            }

            CDTU.Utils.CDLogger.Log($"[BagView] 刷新主动卡视图：{cardGroups.Count} 张卡");
        }

        // public void RefreshPassiveCardViews()
        // {
        //     var inv = InventoryManager.Instance;
        //     if (inv == null)
        //     {
        //         CDTU.Utils.Logger.LogWarning("[BagView] InventoryManager.Instance is null");
        //         return;
        //     }

        //     var passive = inv.PassiveCards;
        //     foreach (var p in passive)
        //     {
        //         if (p.Count <= 0) continue;
        //         _view.AddCardView(p.CardId, p.Count);
        //     }
        // }

        public void RefreshAllCardViews()
        {
            if (_view == null) return;

            _view.ClearCardViews();
            RefreshActiveCardViews();
        }

        public void OnClearCardButtonClicked()
        {
            CDTU.Utils.CDLogger.Log("[BagViewLogic] OnClearCardButtonClicked invoked");
            RefreshAllCardViews();

            // 发布事件请求，由 SlotService 或其他订阅方执行具体清理（实现解耦）
            var playerId = GameRoot.Instance.PlayerManager.GetLocalPlayerRuntimeState()?.PlayerId;
            EventBus.Publish(new RogueGame.Events.ClearAllSlotsRequestedEvent { PlayerId = playerId });
        }


        /// <summary>
        /// 被同层新 UI 覆盖
        /// </summary>
        public virtual void OnCovered()
        {
            // 默认行为：关闭交互
        }

        /// <summary>
        /// 从覆盖状态恢复到栈顶
        /// </summary>
        public virtual void OnResume()
        {
            // 初始化卡牌列表
            RefreshActiveCardViews();
        }

        public void OnPlayerStats1Clicked()
        {
            _view.SetBagViewALLActive(true);
            _view.SetPlayerStatViewActive(false);
            // 初始化卡牌列表
            RefreshActiveCardViews();
        }

        public void OnPlayerStats12Clicked()
        {
            _view.SetBagViewALLActive(false);
            _view.SetPlayerStatViewActive(true);
            // 初始化卡牌列表
            RefreshActiveCardViews();
        }



        public void SetAllPlayerStatsText()
        {
            var pm =GameRoot.Instance.PlayerManager;
            if (pm != null)
            {
                var player = pm.GetLocalPlayerRuntimeState()?.Controller;
                if (player != null)
                {
                    if (localCharacterStats != null)
                    {
                        _view.SetMaxHP("MaxHP: " + localCharacterStats.MaxHP.BaseValue.ToString() + "(" + (localCharacterStats.MaxHP.Value - localCharacterStats.MaxHP.BaseValue).ToString() + ")");
                        _view.SetHPRegen("HPRegen: " + localCharacterStats.HPRegen.BaseValue.ToString() + "(" + (localCharacterStats.HPRegen.Value - localCharacterStats.HPRegen.BaseValue).ToString() + ")");
                        _view.SetMoveSpeed("MoveSpeed: " + localCharacterStats.MoveSpeed.BaseValue.ToString() + "(" + (localCharacterStats.MoveSpeed.Value - localCharacterStats.MoveSpeed.BaseValue).ToString() + ")");
                        _view.SetAcceleration("Acceleration: " + localCharacterStats.Acceleration.BaseValue.ToString() + "(" + (localCharacterStats.Acceleration.Value - localCharacterStats.Acceleration.BaseValue).ToString() + ")");
                        _view.SetAttackPower("AttackPower: " + localCharacterStats.AttackPower.BaseValue.ToString() + "(" + (localCharacterStats.AttackPower.Value - localCharacterStats.AttackPower.BaseValue).ToString() + ")");
                        _view.SetAttackSpeed("AttackSpeed: " + localCharacterStats.AttackSpeed.BaseValue.ToString() + "(" + (localCharacterStats.AttackSpeed.Value - localCharacterStats.AttackSpeed.BaseValue).ToString() + ")");
                        _view.SetAttackRange("AttackRange: " + localCharacterStats.AttackRange.BaseValue.ToString() + "(" + (localCharacterStats.AttackRange.Value - localCharacterStats.AttackRange.BaseValue).ToString() + ")");
                        _view.SetArmor("Armor: " + localCharacterStats.Armor.BaseValue.ToString() + "(" + (localCharacterStats.Armor.Value - localCharacterStats.Armor.BaseValue).ToString() + ")");
                        _view.SetDodge("Dodge: " + localCharacterStats.Dodge.BaseValue.ToString() + "(" + (localCharacterStats.Dodge.Value - localCharacterStats.Dodge.BaseValue).ToString() + ")");
                    }
                }
            }
        }

    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// 继承 UILogicBase 以获得自动事件订阅管理能力
    /// </summary>
    public class BagViewLogic : UILogicBase
    {
        private BagViewLogicCore _core = new BagViewLogicCore();

        public override void Bind(UIViewBase view)
        {
            base.Bind(view);
            _core.Bind(view);
        }

        public override void OnOpen(UIArgs args)
        {
            base.OnOpen(args);
            _core.OnOpen(args);
        }

        public override void OnClose()
        {
            _core.OnClose();
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
    }
}
