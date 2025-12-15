using UnityEngine;


namespace UI.Loading
{
    public static class UIAssetProvider
    {
        public static GameObject Load<T>() where T : UIViewBase
        {
            // 假设 Resources/UI/ 下有对应 prefab
            GameObject prefab = Resources.Load<GameObject>("UI/" + typeof(T).Name);
            if (prefab == null)
            {
                Debug.LogError($"UIAssetProvider: 找不到 {typeof(T).Name} 预制体");
            }
            return prefab;
        }
    }
}