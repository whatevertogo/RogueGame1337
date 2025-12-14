using UnityEngine;

namespace Character.Projectiles
{
    /// <summary>
    /// 投射物池预热器
    /// </summary>
    public class ProjectilePoolWarmer : MonoBehaviour
    {
        [System.Serializable]
        public class WarmupEntry
        {
            public ProjectileConfig config;
            public int count = 10;
        }
        [SerializeField] private WarmupEntry[] entries;
        [SerializeField] private bool warmupOnStart = true;

        private void Start()
        {
            if (warmupOnStart)
            {
                Warmup();
            }
        }

        [ContextMenu("Warmup")]
        public void Warmup()
        {
            if (ProjectilePool.Instance == null)
            {
                Debug.LogWarning("[ProjectilePoolWarmer] ProjectilePool 不存在！");
                return;
            }

            foreach (var entry in entries)
            {
                if (entry.config != null && entry.config.projectilePrefab != null)
                {
                    ProjectilePool.Instance.Warmup(entry.config.projectilePrefab, entry.count);
                }
            }
        }
    }
}