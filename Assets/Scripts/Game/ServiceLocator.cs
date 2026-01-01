using System;
using System.Collections.Generic;
using CDTU.Utils;

/// <summary>
/// 改进版服务定位器
///
/// 功能：
/// 1. 支持命名注册（避免同类型多实例冲突）
/// 2. 支持接口与实现分离注册
/// 3. TryGet 模式（无异常）
/// 4. IsRegistered 检查
/// 5. Clear 方法（用于测试/场景切换）
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// 服务条目：包含服务实例和可选名称
    /// </summary>
    private readonly struct ServiceEntry
    {
        public readonly object Service;
        public readonly string Name;
        public readonly Type RegisteredType;

        public ServiceEntry(object service, string name, Type registeredType)
        {
            Service = service;
            Name = name ?? string.Empty;
            RegisteredType = registeredType;
        }
    }

    /// <summary>
    /// 服务存储：Key 为 (类型, 名称)，Value 为服务条目
    /// </summary>
    private static readonly Dictionary<(Type type, string name), ServiceEntry> services = new Dictionary<(Type, string name), ServiceEntry>();

    /// <summary>
    /// 用于快速检查某类型是否有任何实例（忽略名称）
    /// </summary>
    private static readonly Dictionary<Type, int> typeCount = new Dictionary<Type, int>();

    #region 注册

    /// <summary>
    /// 注册服务（默认名称）
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        Register(service, null);
    }

    /// <summary>
    /// 注册服务（带名称）
    /// 同一类型可以注册多个不同命名的实例
    /// </summary>
    public static void Register<T>(T service, string name) where T : class
    {
        if (service == null)
        {
            CDLogger.LogError($"[ServiceLocator] 尝试注册 null 服务：{typeof(T).Name}");
            return;
        }

        Type serviceType = typeof(T);
        string normalizedName = name ?? string.Empty;
        var key = (serviceType, normalizedName);

        bool isNew = !services.ContainsKey(key);
        services[key] = new ServiceEntry(service, normalizedName, serviceType);

        // 更新类型计数
        if (isNew)
        {
            typeCount.TryGetValue(serviceType, out int count);
            typeCount[serviceType] = count + 1;
        }

        CDLogger.Log($"[ServiceLocator] 注册服务：{serviceType.Name}{(string.IsNullOrEmpty(normalizedName) ? "" : $" ({normalizedName})")}");
    }

    /// <summary>
    /// 注册服务为接口类型
    /// 例如：Register<IRepository, SqlRepository>(repository)
    /// </summary>
    public static void Register<TInterface, TImplementation>(TImplementation service, string name = null)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        if (service == null)
        {
            CDLogger.LogError($"[ServiceLocator] 尝试注册 null 服务：{typeof(TImplementation).Name} as {typeof(TInterface).Name}");
            return;
        }

        Type interfaceType = typeof(TInterface);
        string normalizedName = name ?? string.Empty;
        var key = (interfaceType, normalizedName);

        bool isNew = !services.ContainsKey(key);
        services[key] = new ServiceEntry(service, normalizedName, interfaceType);

        if (isNew)
        {
            typeCount.TryGetValue(interfaceType, out int count);
            typeCount[interfaceType] = count + 1;
        }

        CDLogger.Log($"[ServiceLocator] 注册服务：{typeof(TImplementation).Name} as {interfaceType.Name}{(string.IsNullOrEmpty(normalizedName) ? "" : $" ({normalizedName})")}");
    }

    /// <summary>
    /// 注册服务（指定接口类型）
    /// </summary>
    public static void Register<TInterface>(object service, string name = null) where TInterface : class
    {
        if (service == null)
        {
            CDLogger.LogError($"[ServiceLocator] 尝试注册 null 服务：{typeof(TInterface).Name}");
            return;
        }

        if (!(service is TInterface typedService))
        {
            CDLogger.LogError($"[ServiceLocator] 服务类型不匹配：期望 {typeof(TInterface).Name}，实际 {service.GetType().Name}");
            return;
        }

        Register(typedService, name);
    }

    #endregion

    #region 获取

    /// <summary>
    /// 获取服务（默认名称）
    /// </summary>
    public static T Get<T>() where T : class
    {
        return Get<T>(null);
    }

    /// <summary>
    /// 获取服务（带名称）
    /// </summary>
    public static T Get<T>(string name) where T : class
    {
        Type serviceType = typeof(T);
        string normalizedName = name ?? string.Empty;
        var key = (serviceType, normalizedName);

        if (services.TryGetValue(key, out var entry))
        {
            return (T)entry.Service;
        }

        CDLogger.LogError($"[ServiceLocator] 找不到服务：{serviceType.Name}{(string.IsNullOrEmpty(normalizedName) ? "" : $" ({normalizedName})")}");
        return null;
    }

    /// <summary>
    /// 尝试获取服务（不输出错误日志）
    /// </summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        return TryGet(out service, null);
    }

    /// <summary>
    /// 尝试获取服务（带名称，不输出错误日志）
    /// </summary>
    public static bool TryGet<T>(out T service, string name) where T : class
    {
        service = Get<T>(name);
        return service != null;
    }

    /// <summary>
    /// 获取或创建服务（如果不存在则使用默认构造函数创建）
    /// 警告：仅适用于无参构造函数的服务
    /// </summary>
    public static T GetOrCreate<T>() where T : class, new()
    {
        if (TryGet<T>(out var service))
        {
            return service;
        }

        var newService = new T();
        Register(newService);
        return newService;
    }

    #endregion

    #region 检查

    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    public static bool IsRegistered<T>() where T : class
    {
        return IsRegistered<T>(null);
    }

    /// <summary>
    /// 检查服务是否已注册（带名称）
    /// </summary>
    public static bool IsRegistered<T>(string name) where T : class
    {
        Type serviceType = typeof(T);
        string normalizedName = name ?? string.Empty;
        return services.ContainsKey((serviceType, normalizedName));
    }

    /// <summary>
    /// 检查某类型是否有任何实例注册
    /// </summary>
    public static bool HasAny<T>() where T : class
    {
        return typeCount.ContainsKey(typeof(T)) && typeCount[typeof(T)] > 0;
    }

    /// <summary>
    /// 获取某类型的注册实例数量
    /// </summary>
    public static int Count<T>() where T : class
    {
        return typeCount.TryGetValue(typeof(T), out int count) ? count : 0;
    }

    #endregion

    #region 注销

    /// <summary>
    /// 注销服务（默认名称）
    /// </summary>
    public static bool Unregister<T>() where T : class
    {
        return Unregister<T>(null);
    }

    /// <summary>
    /// 注销服务（带名称）
    /// </summary>
    public static bool Unregister<T>(string name) where T : class
    {
        Type serviceType = typeof(T);
        string normalizedName = name ?? string.Empty;
        var key = (serviceType, normalizedName);

        if (services.Remove(key))
        {
            typeCount[serviceType]--;
            if (typeCount[serviceType] <= 0)
            {
                typeCount.Remove(serviceType);
            }

            CDLogger.Log($"[ServiceLocator] 注销服务：{serviceType.Name}{(string.IsNullOrEmpty(normalizedName) ? "" : $" ({normalizedName})")}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 注销某类型的所有实例
    /// </summary>
    public static int UnregisterAll<T>() where T : class
    {
        Type serviceType = typeof(T);
        int count = 0;

        var keysToRemove = new List<(Type, string)>();
        foreach (var kvp in services)
        {
            if (kvp.Key.type == serviceType)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            services.Remove(key);
            count++;
        }

        typeCount.Remove(serviceType);

        if (count > 0)
        {
            CDLogger.Log($"[ServiceLocator] 注销 {count} 个 {serviceType.Name} 服务");
        }

        return count;
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清空所有服务（主要用于测试/场景切换）
    /// </summary>
    public static void Clear()
    {
        int count = services.Count;
        services.Clear();
        typeCount.Clear();

        if (count > 0)
        {
            CDLogger.Log($"[ServiceLocator] 已清空 {count} 个服务");
        }
    }

    /// <summary>
    /// 获取所有已注册服务的调试信息
    /// </summary>
    public static string GetDebugInfo()
    {
        if (services.Count == 0)
            return "没有已注册的服务";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== 服务定位器状态 ({services.Count} 个服务) ===");

        foreach (var kvp in services)
        {
            var entry = kvp.Value;
            string serviceTypeName = entry.Service?.GetType().Name ?? "null";
            sb.AppendLine($"  {entry.RegisteredType.Name}{(string.IsNullOrEmpty(entry.Name) ? "" : $" [{entry.Name}]")} -> {serviceTypeName}");
        }

        return sb.ToString();
    }

    #endregion
}
