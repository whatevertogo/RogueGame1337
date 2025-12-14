# Unity Utility Collection

🌏 English | [中文](README.zh-CN_Utils.md)

A collection of utility tools developed for Unity projects, providing multiple commonly used utility classes and functions to simplify development processes and improve code quality.

## 📚 Modules

### 🎯 Singleton Pattern

Provides a generic singleton base class with features:

- Automatic instance creation (if none exists)
- Singleton persistence across scene changes (DontDestroyOnLoad)
- Prevention of duplicate instantiation
- Thread safety

Usage example:

```csharp
public class GameManager : Singleton<GameManager> {
    protected override void Awake() {
        base.Awake();
        // Your initialization code
    }
    
    public void GameLogic() {
        // Game logic
    }
}

// Usage elsewhere
GameManager.Instance.GameLogic();
```

### 🎮 Object Pool

An efficient object pooling system to reduce runtime instantiation/destruction performance overhead. Features:

- Supports any Unity Object type through generics
- Automatic object activation state management
- Supports warm-up and dynamic expansion
- Built-in safety check mechanisms
- Optimized memory usage
- Thread-safe design

Usage example:

```csharp
// Bullet system example
public class BulletSystem : MonoBehaviour 
{
    [SerializeField] private GameObject bulletPrefab;
    private ObjectPool<GameObject> bulletPool;
    
    private void Awake() 
    {
        // Initialize pool with 20 pre-instantiated objects
        bulletPool = new ObjectPool<GameObject>(bulletPrefab, 20, transform);
    }
    
    public void FireBullet(Vector3 position, Vector3 direction) 
    {
        // Get bullet from pool
        var bullet = bulletPool.Get();
        bullet.transform.position = position;
        bullet.transform.forward = direction;
        
        // Set recycle timer
        StartCoroutine(ReturnBulletToPool(bullet, 3f));
    }
    
    private IEnumerator ReturnBulletToPool(GameObject bullet, float delay) 
    {
        yield return new WaitForSeconds(delay);
        bulletPool.Release(bullet);
    }
    
    private void OnDestroy() 
    {
        // Clean up pool
        bulletPool.Clear(true);
    }
}
```

### 🛠️ Extension Methods

Useful extension methods for common Unity types:

```csharp
// Transform extensions
transform.Reset(); // Reset transform
transform.SetGlobalScale(Vector3.one); // Set global scale

// GameObject extensions
gameObject.SetLayerRecursively(LayerMask.NameToLayer("UI")); // Recursive layer setting
gameObject.SetActiveOptimized(false); // Optimized SetActive call

// Component extensions
var comp = GetComponentOptimized<T>(); // Cached component access
```

## 💡 Performance Optimization Best Practices

### Object Pool Usage Guidelines

1. Warm-up Timing
   - Warm up during level loading
   - Set initial pool size based on usage statistics
   - Avoid frequent runtime expansion
2. Memory Management
   - Use Clear(true) at appropriate times
   - Monitor pool size regularly
   - Set parent transforms for easy debugging

3. Multi-Pool Management

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

## 🔧 Installation

1. Copy the `Utils` folder into your project's `Assets` folder
2. Add the relevant namespace references:

```csharp
using Utils;       // Object pool and utilities
using Utils.Math;  // Math utilities
using Utils.Debug; // Debug tools
```

## 📝 Important Notes

1. Object Pool Usage:
   - Objects must implement proper reset logic
   - Ensure objects aren't referenced elsewhere before Release
   - Avoid frequent Get/Release in Update  
2. Debug Tool Usage:
   - Enable performance monitoring in development only
   - Use conditional compilation directives appropriately
   - Clean up debug logs regularly

## License

This project is licensed under the MIT License - see the LICENSE file for details
