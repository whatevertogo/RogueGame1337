using System;
using UnityEngine;
using UI;
using RogueGame.Game.Service;

namespace Game.UI
{
    /// <summary>
    /// ShopUIView 纯逻辑核心（可单元测试）
    /// </summary>
    public class ShopUIViewLogicCore
    {
        protected ShopUIView _view;

        private ShopType _recentShopType;

        private int _spendCoins;

        public virtual void Bind(UIViewBase view)
        {
            _view = view as ShopUIView;
            ShopManager.Instance.OnItemPurchased += OnItemPurchasedMethod;
        }

        private void OnItemPurchasedMethod(string arg1, int arg2)
        {
            UpdateCoinText();
        }

        public virtual void OnOpen(UIArgs args)
        {
            var shopArg = args as ShopUIArgs;

            if (_view == null) return;

            // 记录最近的 ShopType 并消费金额
            if (shopArg != null)
            {
                _recentShopType = shopArg.ShopType;
                _spendCoins = shopArg.SpendCoins;
                _view.SetShopType(shopArg.ShopMessage ?? string.Empty);
            }
            else
            {
                _view.SetShopType(string.Empty);
            }

            UpdateCoinText();
        }

        public virtual void OnClose()
        {
            // 关闭时清理
            ShopManager.Instance.OnItemPurchased -= OnItemPurchasedMethod;
            _view = null;
        }

        public virtual void OnCovered()
        {
            // 被同层新 UI 覆盖时的默认处理（子类可重写）
        }

        public virtual void OnResume()
        {
            // 从覆盖状态恢复时的默认处理（子类可重写）
            UpdateCoinText();
        }

        private void UpdateCoinText()
        {
            if (_view != null)
            {
                int coinNumber = GameRoot.Instance.InventoryManager.Coins;
                _view.SetCoinText($"Coins: {coinNumber}");
            }
        }

        public void OnButton1Clicked()
        {
            if (ShopManager.Instance == null)
            {
                CDTU.Utils.CDLogger.LogWarning("[ShopUIViewLogic] ShopManager.Instance is null");
                return;
            }

            switch (_recentShopType)
            {
                case ShopType.BloodShop:
                    ShopManager.Instance.BuyBloods(_spendCoins);
                    break;
                case ShopType.CardShop:
                    ShopManager.Instance.BuyCards(_spendCoins);
                    break;
            }
        }
    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// 继承 UILogicBase 以获得自动事件订阅管理能力
    /// </summary>
    public class ShopUIViewLogic : UILogicBase
    {
        private ShopUIViewLogicCore _core = new ShopUIViewLogicCore();
        private ShopUIView _view;

        public override void Bind(UIViewBase view)
        {
            base.Bind(view);
            _core.Bind(view);
            _view = view as ShopUIView;
            if (_view != null) _view.BindButton1Button(OnButton1Clicked);
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

        private void OnButton1Clicked()
        {
            _core.OnButton1Clicked();
        }
    }
}
