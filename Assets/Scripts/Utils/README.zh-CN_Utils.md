# Unity 实用工具集合

[English](README.EN_Utils.md) | 🌏 中文

这是一个为 Unity 项目开发的实用工具集合，提供了多个常用的工具类和函数，帮助简化开发流程，提高代码质量。

## 📚 功能模块

### 🎯 单例模式 (Singleton)

提供了一个通用的单例模式基类，特点：

- 自动创建实例（如果不存在）
- 场景切换时保持单例存活（DontDestroyOnLoad）
- 防止重复实例化
- 线程安全

使用示例：

```csharp
public class GameManager : Singleton<GameManager> {
    protected override void Awake() {
        base.Awake();
        // 你的初始化代码
    }
    
    public void GameLogic() {
        // 游戏逻辑
    }
}

// 在其他地方使用
GameManager.Instance.GameLogic();
```

### 🎮 对象池 (ObjectPool)

高效的对象池系统，用于减少运行时实例化/销毁对象的性能开销。特点：

- 支持任意 Unity Object 类型，通过范型实现
- 自动管理对象激活状态
- 支持预热和动态扩容
- 内置安全检查机制
- 优化的内存使用
- 线程安全设计

使用示例：

```csharp
// 子弹系统示例
public class BulletSystem : MonoBehaviour 
{
    [SerializeField] private GameObject bulletPrefab;
    private ObjectPool<GameObject> bulletPool;
    
    private void Awake() 
    {
        // 初始化对象池，预创建20个对象
        bulletPool = new ObjectPool<GameObject>(bulletPrefab, 20, transform);
    }
    
    public void FireBullet(Vector3 position, Vector3 direction) 
    {
        // 从池中获取子弹
        var bullet = bulletPool.Get();
        bullet.transform.position = position;
        bullet.transform.forward = direction;
        
        // 设置回收计时器
        StartCoroutine(ReturnBulletToPool(bullet, 3f));
    }
    
    private IEnumerator ReturnBulletToPool(GameObject bullet, float delay) 
    {
        yield return new WaitForSeconds(delay);
        bulletPool.Release(bullet);
    }
    
    private void OnDestroy() 
    {
        // 清理对象池
        bulletPool.Clear(true);
    }
}
```

### 🛠️ 扩展方法

为常用Unity类型提供实用的扩展方法：

```csharp
// Transform 扩展示例
transform.Reset(); // 重置变换
transform.SetGlobalScale(Vector3.one); // 设置全局缩放

// GameObject 扩展示例
gameObject.SetLayerRecursively(LayerMask.NameToLayer("UI")); // 递归设置层
gameObject.SetActiveOptimized(false); // 优化的SetActive调用

// Component 扩展示例
var comp = GetComponentOptimized<T>(); // 缓存优化的组件获取
```

```

## 💡 性能优化最佳实践

### 对象池使用建议
1. 预热时机选择
   - 关卡加载时预热
   - 根据统计数据设置初始池大小
   - 避免运行时频繁扩容
   
2. 内存管理
   - 使用Clear(true)在适当时机清理池
   - 定期监控池大小，避免内存泄漏
   - 合理设置父物体，方便调试和管理

3. 多对象池管理
   ```csharp
   public class ObjectPoolManager : Singleton<ObjectPoolManager> 
   {
       private Dictionary<string, ObjectPool<GameObject>> pools = new();
       
       public ObjectPool<GameObject> GetPool(string key, GameObject prefab, int defaultSize = 10) 
       {
           if (!pools.TryGetValue(key, out var pool)) 
           {
               pool = new ObjectPool<GameObject>(prefab, defaultSize, transform);
               pools.Add(key, pool);
           }
           return pool;
       }
       
       public void ClearAll() 
       {
           foreach (var pool in pools.Values) 
           {
               pool.Clear(true);
           }
           pools.Clear();
       }
   }
   ```

## 🔧 安装使用

1. 将 `Utils` 文件夹复制到你的项目的 `Assets` 文件夹中
2. 添加相应的命名空间引用：

```csharp
using Utils;       // 对象池和通用工具
using Utils.Math;  // 数学工具
using Utils.Debug; // 调试工具
```

## 📝 注意事项

1. 对象池使用时注意：
   - 对象必须实现正确的重置逻辑
   - Release前确保对象没有被其他系统引用
   - 避免在Update中频繁Get/Release对象  
2. 调试工具使用建议：
   - 仅在开发环境启用性能监控
   - 合理使用条件编译指令
   - 定期清理调试日志

## 许可证

本项目采用 MIT 许可证 - 详情请查看 LICENSE 文件
