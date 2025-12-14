# 对象池系统(ObjectPool)

一个简单高效的Unity对象池系统，可减少游戏过程中频繁实例化和销毁对象所带来的性能开销。

## 特点

- 泛型实现，适用于任何Unity Object类型
- 自动管理池化对象的激活状态
- 支持预热功能，可在启动时初始化对象
- 需要时动态扩展池容量
- 集合检查，防止常见的池化错误
- 活跃对象追踪

## 使用方法

### 基本使用

```csharp
// 为GameObject预制体创建一个对象池
public GameObject bulletPrefab;
private ObjectPool<GameObject> bulletPool;

void Start()
{
    // 初始化池，默认大小为20个子弹
    bulletPool = new ObjectPool<GameObject>(bulletPrefab, 20, transform);
}

// 从池中获取一个对象:
void ShootBullet()
{
    GameObject bullet = bulletPool.Get();
    // 配置子弹（位置、方向等）
    bullet.transform.position = firePoint.position;
    bullet.transform.rotation = firePoint.rotation;
}

// 将对象返回到池中:
void OnBulletFinished(GameObject bullet)
{
    bulletPool.Release(bullet);
}
```

### 构造函数参数

```csharp
public ObjectPool(T prefab, int defaultSize = 10, Transform parent = null, bool collectionChecks = true)
```

- `prefab`: 要实例化的预制体
- `defaultSize`: 初始池大小（默认值：10）
- `parent`: 池化对象的父级Transform（默认值：null）
- `collectionChecks`: 是否执行错误回收检查（默认值：true）

### 可用方法

- `Get()`: 从池中获取一个对象
- `Release(T obj)`: 将对象返回到池中
- `Warmup(int count)`: 预先实例化指定数量的对象
- `Clear(bool destroyActive = false)`: 清空池，并可选择销毁活跃对象

### 属性

- `CountInactive`: 池中可用对象数量
- `CountActive`: 当前正在使用的对象数量
- `CountAll`: 此池管理的对象总数

## 最佳实践

1. **在游戏开始前预热对象池**，避免瞬时实例化导致的性能卡顿
2. **适当调整池的大小**，以适应预期用量，最小化内存消耗
3. **使用完对象后务必将其归还**到对象池
4. **考虑实现一个超时系统**，处理那些可能没有自然归还的对象

## 技术细节

该实现使用队列存储非活跃对象，并使用哈希集合跟踪活跃对象。这样可以快速访问池化对象，同时保持执行集合完整性检查的能力。

## 集成示例

### 与子弹或投射物结合

```csharp
public class BulletManager : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 30;
    private ObjectPool<GameObject> bulletPool;
    
    private void Awake()
    {
        bulletPool = new ObjectPool<GameObject>(bulletPrefab, poolSize, transform);
    }
    
    public GameObject GetBullet()
    {
        return bulletPool.Get();
    }
    
    public void ReturnBullet(GameObject bullet)
    {
        bulletPool.Release(bullet);
    }
}
```

### 与粒子效果结合

```csharp
public class ParticleEffectPool : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    private ObjectPool<GameObject> effectPool;
    
    private void Start()
    {
        effectPool = new ObjectPool<GameObject>(explosionPrefab, 10, transform);
    }
    
    public void PlayExplosionAt(Vector3 position)
    {
        GameObject effect = effectPool.Get();
        effect.transform.position = position;
        
        // 粒子系统完成后自动返回到池中
        StartCoroutine(ReturnToPoolAfterPlay(effect));
    }
    
    private IEnumerator ReturnToPoolAfterPlay(GameObject effect)
    {
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
        effectPool.Release(effect);
    }
}
```