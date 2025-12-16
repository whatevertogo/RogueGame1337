using System;
using System.Collections.Generic;
using UI.Loading;
using UnityEngine;
using CDTU.Utils;

namespace UI
{
    /// <summary>
    /// UI管理器 - 单例模式管理所有UI界面
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private Dictionary<Type, UIViewBase> _openViews = new();
        private readonly Dictionary<UILayer, Transform> _layerRoots = new();

        protected override void Awake()
        {
            base.Awake();
            
            // 创建各层根节点
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                GameObject root = new GameObject(layer.ToString() + "Layer");
                // 使用 false 保持本地变换一致性（避免 world position 被修改）
                root.transform.SetParent(transform, false);
                _layerRoots[layer] = root.transform;
            }
            
            Debug.Log("[UIManager] UI管理器初始化完成");
        }

        /// <summary>
        /// 获取指定层的根节点
        /// </summary>
        /// <param name="layer">UI层</param>
        /// <returns>层根节点Transform</returns>
        public Transform GetLayerRoot(UILayer layer)
        {
            return _layerRoots.ContainsKey(layer) ? _layerRoots[layer] : transform;
        }

        /// <summary>
        /// 打开 UI 并绑定逻辑模块
        /// </summary>
        public T Open<T>(UIArgs args = null, UILayer layer = UILayer.Normal, params IUILogic[] logics)
            where T : UIViewBase
        {
            Type viewType = typeof(T);

            if (_openViews.TryGetValue(viewType, out var existView))
            {
                Debug.LogWarning($"{viewType} 已经打开");
                return existView as T;
            }

            // 假设资源路径 = T.Name
            GameObject prefab = UIAssetProvider.Load<T>();
            if (prefab == null)
            {
                Debug.LogError($"无法加载 UI 预制体：{viewType},是否路径或名称错误？");
                return null;
            }
            else
            {
                Debug.Log($"加载 UI 预制体：{viewType}");
            }
            Debug.Log("实施了Open方法");    

            GameObject instance = Instantiate(prefab, _layerRoots[layer], false);
            T view = instance.GetComponent<T>();
            view.OnCreate();

            // 自动绑定预制体/实例上实现 IUILogic 的 MonoBehaviour（如果有的话）
            var monos = instance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in monos)
            {
                if (mb is IUILogic logicComponent)
                {
                    view.AddLogic(logicComponent);
                }
            }

            // 绑定逻辑
            if (logics != null)
            {
                foreach (var logic in logics)
                {
                    view.AddLogic(logic);
                }
            }

            view.OnOpen(args);
            _openViews[viewType] = view;
            return view;
        }

        public void Close<T>() where T : UIViewBase
        {
            Type viewType = typeof(T);
            if (_openViews.TryGetValue(viewType, out var view))
            {
                view.OnClose();
                Destroy(view.gameObject);
                _openViews.Remove(viewType);
            }
        }

        public bool IsOpen<T>() where T : UIViewBase
        {
            return _openViews.ContainsKey(typeof(T));
        }
    }
}