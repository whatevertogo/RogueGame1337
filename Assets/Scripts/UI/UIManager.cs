using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using UI.Loading;

namespace UI
{
    /// <summary>
    /// UI 管理器
    /// - Layer 由 UIManager 统一管理
    /// - 每个 Layer 一个 View 栈（支持 ESC 回退）
    /// - View 不感知 Layer
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private class ViewEntry
        {
            public UIViewBase View;
            public UILayer Layer;
        }

        /// <summary>
        /// 当前已打开的 View（用于快速查询）
        /// </summary>
        private readonly Dictionary<Type, ViewEntry> _openViews = new();

        /// <summary>
        /// 每个 Layer 对应一个 View 栈（用于导航 / 回退）
        /// </summary>
        private readonly Dictionary<UILayer, Stack<UIViewBase>> _layerStacks = new();

        /// <summary>
        /// 每个 Layer 对应一个根节点
        /// </summary>
        private readonly Dictionary<UILayer, Transform> _layerRoots = new();

        protected override void Awake()
        {
            base.Awake();
            CreateLayerRoots();
            Debug.Log("[UIManager] Initialized");
        }

        #region Layer 管理

        private void CreateLayerRoots()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var go = new GameObject(layer + "Layer");
                go.transform.SetParent(transform, false);

                _layerRoots[layer] = go.transform;
                _layerStacks[layer] = new Stack<UIViewBase>();
            }
        }

        private Transform GetLayerRoot(UILayer layer)
        {
            return _layerRoots.TryGetValue(layer, out var root)
                ? root
                : transform;
        }

        #endregion

        #region Open / Close

        /// <summary>
        /// 打开 UI
        /// </summary>
        public T Open<T>(
            UIArgs args = null,
            UILayer layer = UILayer.Normal,
            params IUILogic[] logics
        ) where T : UIViewBase
        {
            Type type = typeof(T);

            // 已经打开
            if (_openViews.TryGetValue(type, out var exist))
            {
                Debug.LogWarning($"[UIManager] {type.Name} already opened");
                return exist.View as T;
            }

            // 加载 prefab
            GameObject prefab = UIAssetProvider.Load<T>();
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] UI prefab not found: {type.Name}");
                return null;
            }

            // 实例化
            Transform root = GetLayerRoot(layer);
            GameObject instance = Instantiate(prefab, root, false);

            T view = instance.GetComponent<T>();
            if (view == null)
            {
                Debug.LogError($"[UIManager] {type.Name} missing UIViewBase");
                Destroy(instance);
                return null;
            }

            // 同层互斥
            if (view.Exclusive)
            {
                CloseAllInLayer(layer);
            }

            // 覆盖当前栈顶
            var stack = _layerStacks[layer];
            if (stack.TryPeek(out var covered))
            {
                covered.OnCovered();
            }

            // 生命周期
            view.OnCreate();

            // 自动收集 Logic
            BindLogicFromHierarchy(view, instance);

            // 手动注入 Logic
            if (logics != null)
            {
                foreach (var logic in logics)
                {
                    view.AddLogic(logic);
                }
            }

            view.OnOpen(args);

            // 入栈
            stack.Push(view);

            // 注册
            _openViews[type] = new ViewEntry
            {
                View = view,
                Layer = layer
            };

            return view;
        }

        public T GetView<T>() where T : UIViewBase
        {
            Type type = typeof(T);
            if (_openViews.TryGetValue(type, out var entry))
            {
                return entry.View as T;
            }
            return null;
        }

        /// <summary>
        /// 关闭栈顶 UI（ESC / 返回）
        /// </summary>
        public void CloseTop(UILayer layer = UILayer.Normal)
        {
            var stack = _layerStacks[layer];
            if (stack.Count <= 1)
                return;

            var top = stack.Pop();
            top.OnClose();
            Destroy(top.gameObject);

            _openViews.Remove(top.GetType());

            // 恢复下一个
            stack.Peek().OnResume();
        }

        /// <summary>
        /// 关闭指定 UI
        /// </summary>
        public void Close<T>() where T : UIViewBase
        {
            Type type = typeof(T);

            if (!_openViews.TryGetValue(type, out var entry))
                return;

            var layer = entry.Layer;
            var stack = _layerStacks[layer];

            // 如果是栈顶，走标准回退流程
            if (stack.Count > 0 && stack.Peek() == entry.View)
            {
                CloseTop(layer);
                return;
            }

            // 非栈顶（极少使用）
            entry.View.OnClose();
            Destroy(entry.View.gameObject);
            _openViews.Remove(type);
        }

        /// <summary>
        /// 关闭某一层的所有 UI，包括栈顶和非栈顶
        /// </summary>
        private void CloseAllInLayer(UILayer layer)
        {
            var stack = _layerStacks[layer];

            while (stack.Count > 0)
            {
                var view = stack.Pop();
                if (view == null) continue;

                view.OnClose();
                Destroy(view.gameObject);
                _openViews.Remove(view.GetType());
            }
        }

        #endregion

        #region Back / 查询

        /// <summary>
        /// 处理返回（ESC）
        /// </summary>
        public bool HandleBack(UILayer layer = UILayer.Normal)
        {
            var stack = _layerStacks[layer];
            if (stack.Count <= 1)
                return false;

            if (!stack.Peek().CanBack)
                return false;

            CloseTop(layer);
            return true;
        }

        public bool IsOpen<T>() where T : UIViewBase
        {
            return _openViews.ContainsKey(typeof(T));
        }

        public bool HasViewInLayer(UILayer layer)
        {
            return _layerStacks[layer].Count > 0;
        }

        #endregion

        #region Logic 绑定

        private void BindLogicFromHierarchy(UIViewBase view, GameObject root)
        {
            var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in monos)
            {
                if (mb is IUILogic logic)
                {
                    view.AddLogic(logic);
                }
            }
        }

        #endregion
    }
}
