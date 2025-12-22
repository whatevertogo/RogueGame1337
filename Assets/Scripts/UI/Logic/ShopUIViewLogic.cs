using System;
using UnityEngine;
using UI;

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
        }

        public virtual void OnOpen(UIArgs args)
        {
            var shopArg = args as ShopUIArg;

            if (_view is null) return;
            if (shopArg is null)
            {
                _view.SetShopType(string.Empty);
                return;
            }
            // 记录最近的 ShopType 并消费金额
            _recentShopType = shopArg.ShopType;
            _spendCoins = shopArg.SpendCoins;

            int coinNumber = InventoryManager.Instance != null ? InventoryManager.Instance.CoinsNumber : 0;
            _view.SetCoinText($"Coins: {coinNumber}");
            _view.SetShopType(shopArg.ShopMessage ?? string.Empty);
        }

        public virtual void OnClose()
        {
            // 关闭时清理
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

        public void OnButton1Clicked()
        {
            if (ShopManager.Instance == null)
            {
                Debug.LogWarning("[ShopUIViewLogic] ShopManager.Instance is null");
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
    /// </summary>
    public class ShopUIViewLogic : MonoBehaviour, IUILogic
    {
        private ShopUIViewLogicCore _core = new ShopUIViewLogicCore();
        private ShopUIView _view;

        public void Bind(UIViewBase view)
        {
            _core.Bind(view);
            _view = view as ShopUIView;
            if (_view != null) _view.BindButton1Button(OnButton1Clicked);
        }

        public void OnOpen(UIArgs args)
        {
            _core.OnOpen(args);
        }

        public void OnClose()
        {
            _core.OnClose();
            if (_view != null) _view.BindButton1Button(null);
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

        private void OnButton1Clicked()
        {
            _core.OnButton1Clicked();
        }
    }
}
