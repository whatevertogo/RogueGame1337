# ObjectPool System

A simple and efficient object pooling system for Unity that helps reduce the performance overhead of instantiating and destroying objects frequently during gameplay.

## Features

- Generic implementation that works with any Unity Object type
- Automatic active state management of pooled objects
- Pre-warming capability to initialize objects at startup
- Dynamic pool expansion when needed
- Collection checks to prevent common pooling errors
- Active object tracking

## How to Use

### Basic Usage

```csharp
// Create an object pool for a GameObject prefab
public GameObject bulletPrefab;
private ObjectPool<GameObject> bulletPool;

void Start()
{
    // Initialize the pool with a default size of 20 bullets
    bulletPool = new ObjectPool<GameObject>(bulletPrefab, 20, transform);
}

// To get an object from the pool:
void ShootBullet()
{
    GameObject bullet = bulletPool.Get();
    // Configure the bullet (position, direction, etc.)
    bullet.transform.position = firePoint.position;
    bullet.transform.rotation = firePoint.rotation;
}

// To return an object to the pool:
void OnBulletFinished(GameObject bullet)
{
    bulletPool.Release(bullet);
}
```

### Constructor Parameters

```csharp
public ObjectPool(T prefab, int defaultSize = 10, Transform parent = null, bool collectionChecks = true)
```

- `prefab`: The prefab to instantiate for the pool
- `defaultSize`: Initial pool size (default: 10)
- `parent`: Transform to parent the pooled objects to (default: null)
- `collectionChecks`: Whether to perform error checking for incorrect releases (default: true)

### Available Methods

- `Get()`: Get an object from the pool
- `Release(T obj)`: Return an object to the pool
- `Warmup(int count)`: Pre-instantiate a specified number of objects
- `Clear(bool destroyActive = false)`: Empty the pool and optionally destroy active objects

### Properties

- `CountInactive`: Number of objects available in the pool
- `CountActive`: Number of objects currently in use
- `CountAll`: Total number of objects managed by this pool

## Best Practices

1. **Pre-warm your pools** before gameplay starts to avoid instantiation lag spikes
2. **Size your pools appropriately** for your expected usage to minimize memory consumption
3. **Always release objects** back to the pool when done with them
4. **Consider implementing a timeout** system for objects that might not get naturally released

## Technical Details

The implementation uses a queue for inactive objects and a hashset to track active objects. This allows for quick access to pooled objects while maintaining the ability to perform collection integrity checks.

## Integration Examples

### With Bullets or Projectiles

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

### With Particle Effects

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
        
        // Automatically return to pool when particle system completes
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