using UnityEngine;
using Character;

namespace RogueGame.Map.Factory
{
    /// <summary>
    /// 一个负责实例化敌人并注入配置/初始数据的简单工厂
    /// - 负责：Instantiate prefab, 可选注入 EnemyConfigSO, 传入当前 floor
    /// - 不负责：房间注册、敌人列表管理（由 RoomController 处理）
    /// </summary>
    public static class EnemyFactory
    {
        public static GameObject Spawn(GameObject prefab, Vector3 position, Transform parent, EnemyConfigSO configOverride=null, int floor = 1)
        {
            if (prefab == null) return null;

            // 实例化
            var go = Object.Instantiate(prefab, position, Quaternion.identity, parent);

            // 注入/覆盖配置（若提供）
            var enemyChar = go.GetComponent<EnemyCharacter>();
            if (enemyChar != null && configOverride != null)
            {
                enemyChar.OverrideConfig(configOverride, floor);
            }

            return go;
        }
    }
}
