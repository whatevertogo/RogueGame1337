using System.Collections;
using UnityEngine;

/// <summary>
/// 自动回收特效到 VFXPool
/// </summary>
public class VFXAutoRelease : MonoBehaviour
{
    [Tooltip("强制回收时间（秒），<=0 时自动根据粒子持续时间计算")]
    [SerializeField] private float overrideDuration = -1f;

    private VFXPool _pool;
    private GameObject _prefabKey;
    private Coroutine _releaseRoutine;

    /// <summary>
    /// 初始化并开始计时
    /// </summary>
    public void Init(VFXPool pool, GameObject prefabKey, float? customDuration = null)
    {
        _pool = pool;
        _prefabKey = prefabKey;

        StartReleaseTimer(customDuration ?? overrideDuration);
    }

    public void Release()
    {
        StopTimer();

        if (_pool != null)
        {
            _pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void StartReleaseTimer(float duration)
    {
        StopTimer();

        float finalDuration = duration <= 0f ? CalculateDuration() : duration;
        _releaseRoutine = StartCoroutine(ReleaseAfter(finalDuration));
    }

    private void StopTimer()
    {
        if (_releaseRoutine != null)
        {
            StopCoroutine(_releaseRoutine);
            _releaseRoutine = null;
        }
    }

    private IEnumerator ReleaseAfter(float time)
    {
        yield return new WaitForSeconds(time);
        Release();
    }

    private float CalculateDuration()
    {
        float maxDuration = 0f;
        var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            // 持续时间 + 最长生命周期，兼容循环/一次性发射
            float duration = main.duration + main.startLifetime.constantMax;
            if (duration > maxDuration)
                maxDuration = duration;
        }

        // 兜底时间，避免配置异常导致提前回收
        if (maxDuration <= 0f)
            maxDuration = 2f;

        return maxDuration;
    }

    private void OnDisable()
    {
        StopTimer();
    }
}
