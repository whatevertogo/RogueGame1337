using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 通用的BFS（广度优先搜索）工具类
/// </summary>
/// <typeparam name="T">节点类型</typeparam>
public class BFSUtil<T>
{
    /// <summary>
    /// 从起始节点开始执行广度优先搜索(BFS)
    /// </summary>
    /// <param name="start">搜索的起始节点</param>
    /// <param name="getNeighbors">获取节点邻接关系的委托函数</param>
    /// <param name="process">处理每个访问到的节点的委托函数</param>
    public void BFS(T start, Func<T, IEnumerable<T>> getNeighbors, Action<T> process)
    {
        // 检查起始节点是否为空
        if (start == null) return;
        
        // 初始化队列(用于BFS遍历)和已访问集合(用于记录已访问节点)
        Queue<T> queue = new Queue<T>();
        HashSet<T> visited = new HashSet<T>();
        
        // 将起始节点加入队列并标记为已访问
        queue.Enqueue(start);
        visited.Add(start);
        
        // 主循环：当队列不为空时继续处理
        while (queue.Count > 0)
        {
            // 从队列头部取出当前节点
            T current = queue.Dequeue();
            
            // 处理当前节点（执行用户定义的操作）
            process(current);
            
            // 获取当前节点的所有邻接节点并遍历
            foreach (T neighbor in getNeighbors(current))
            {
                // 如果邻接节点未被访问过
                if (!visited.Contains(neighbor))
                {
                    // 标记为已访问并加入队列尾部
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
    
    /// <summary>
    /// 带层级信息的BFS遍历，在遍历每个节点时会同时提供该节点所在的层级(距离起始节点的步数)
    /// </summary>
    /// <param name="start">起始节点</param>
    /// <param name="getNeighbors">获取邻接节点的函数，输入当前节点，返回该节点的所有邻接节点</param>
    /// <param name="levelProcess">处理节点和其层级的回调函数，第一个参数是当前节点，第二个参数是该节点所在的层级(从0开始)</param>
    public void BFSWithLevel(T start, Func<T, IEnumerable<T>> getNeighbors, Action<T, int> levelProcess)
    {
        // 检查起始节点是否为空，避免空引用异常
        if (start == null) return;
        
        // 初始化BFS所需的队列、已访问集合和层级记录字典
        Queue<T> queue = new Queue<T>();          // 用于存储待处理的节点
        HashSet<T> visited = new HashSet<T>();    // 记录已访问过的节点，避免重复处理
        Dictionary<T, int> levels = new Dictionary<T, int>(); // 记录每个节点所在的层级
        
        // 将起始节点加入队列，标记为已访问，并设置其层级为0
        queue.Enqueue(start);
        visited.Add(start);
        levels[start] = 0;
        
        // BFS主循环，当队列不为空时继续处理
        while (queue.Count > 0)
        {
            // 取出队列头部的当前节点
            T current = queue.Dequeue();
            int currentLevel = levels[current];  // 获取当前节点的层级
            
            // 调用用户提供的处理函数，传入当前节点及其层级
            levelProcess(current, currentLevel);
            
            // 遍历当前节点的所有邻接节点
            foreach (T neighbor in getNeighbors(current))
            {
                // 如果邻接节点未被访问过
                if (!visited.Contains(neighbor))
                {
                    // 标记为已访问并加入队列尾部
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    // 设置邻接节点的层级为当前节点层级+1
                    levels[neighbor] = currentLevel + 1;
                }
            }
        }
    }
    
    /// <summary>
    /// 寻找从起点到终点的最短路径（使用BFS算法）
    /// </summary>
    /// <param name="start">路径起点</param>
    /// <param name="end">路径终点</param>
    /// <param name="getNeighbors">获取节点邻接关系的委托函数</param>
    /// <param name="comparer">自定义节点比较器，默认为EqualityComparer<T>.Default</param>
    /// <returns>从起点到终点的最短路径列表，如果不存在路径则返回空列表</returns>
    public List<T> FindShortestPath(T start, T end, Func<T, IEnumerable<T>> getNeighbors, IEqualityComparer<T> comparer = null)
    {
        // 边界检查：如果起点或终点为空，直接返回空路径
        if (start == null || end == null) return new List<T>();
        
        // 设置比较器，如果未提供则使用默认比较器
        comparer = comparer ?? EqualityComparer<T>.Default;
        
        // 特殊情况：起点和终点相同，直接返回包含起点的单元素路径
        if (comparer.Equals(start, end)) return new List<T> { start };
        
        // 初始化BFS数据结构：
        Queue<T> queue = new Queue<T>();                     // 用于BFS遍历的队列
        Dictionary<T, T> parentMap = new Dictionary<T, T>(comparer); // 记录每个节点的父节点（用于路径回溯）
        HashSet<T> visited = new HashSet<T>(comparer);       // 记录已访问节点
        
        // 将起点加入队列并标记为已访问
        queue.Enqueue(start);
        visited.Add(start);
        
        bool found = false;  // 是否找到终点的标志
        while (queue.Count > 0 && !found)
        {
            T current = queue.Dequeue();
            
            // 遍历当前节点的所有邻接节点
            foreach (T neighbor in getNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    parentMap[neighbor] = current;  // 记录邻接节点的父节点为当前节点
                    
                    // 检查是否到达终点
                    if (comparer.Equals(neighbor, end))
                    {
                        found = true;  // 设置找到标志并跳出循环
                        break;
                    }
                }
            }
        }
        
        // 如果找不到路径，返回空列表
        if (!found) return new List<T>();
        
        // 回溯重建路径：从终点开始，通过parentMap回溯到起点
        List<T> path = new List<T>();
        T currentNode = end;
        
        while (true)
        {
            path.Add(currentNode);
            
            // 如果回溯到起点，终止循环
            if (comparer.Equals(currentNode, start))
                break;
                
            currentNode = parentMap[currentNode];  // 获取当前节点的父节点
        }
        
        // 反转路径，得到从起点到终点的正确顺序
        path.Reverse();
        return path;
    }
    
    /// <summary>
    /// 寻找满足特定条件的节点
    /// </summary>
    /// <param name="start">起始节点</param>
    /// <param name="getNeighbors">获取邻接节点的函数</param>
    /// <param name="predicate">判断节点是否满足条件的函数</param>
    /// <returns>满足条件的节点，如果不存在则返回默认值</returns>
    /// <summary>
    /// 从起始节点开始，使用广度优先搜索（BFS）算法寻找满足特定条件的第一个节点。
    /// </summary>
    /// <param name="start">搜索的起始节点。</param>
    /// <param name="getNeighbors">一个委托函数，用于获取给定节点的所有邻接节点。</param>
    /// <param name="predicate">一个委托函数，用于判断节点是否满足特定条件。</param>
    /// <returns>满足条件的第一个节点，如果未找到则返回类型的默认值。</returns>
    public T FindNode(T start, Func<T, IEnumerable<T>> getNeighbors, Func<T, bool> predicate)
    {
        // 若起始节点为空，直接返回类型的默认值
        if (start == null) return default;
        
        // 初始化用于 BFS 遍历的队列，存储待处理的节点
        Queue<T> queue = new Queue<T>();
        // 初始化已访问节点的集合，避免重复处理节点
        HashSet<T> visited = new HashSet<T>();
        
        // 将起始节点加入队列
        queue.Enqueue(start);
        // 标记起始节点为已访问
        visited.Add(start);
        
        // 主循环：当队列不为空时继续搜索
        while (queue.Count > 0)
        {
            // 从队列头部取出当前节点
            T current = queue.Dequeue();
            
            // 检查当前节点是否满足给定条件
            if (predicate(current))
                // 若满足条件，返回该节点
                return current;
                
            // 遍历当前节点的所有邻接节点
            foreach (T neighbor in getNeighbors(current))
            {
                // 若邻接节点未被访问过
                if (!visited.Contains(neighbor))
                {
                    // 标记邻接节点为已访问
                    visited.Add(neighbor);
                    // 将邻接节点加入队列，等待后续处理
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        // 若未找到满足条件的节点，返回类型的默认值
        return default;
    }
    
    /// <summary>
    /// 寻找满足特定条件的所有节点
    /// </summary>
    /// <param name="start">起始节点</param>
    /// <param name="getNeighbors">获取邻接节点的函数</param>
    /// <param name="predicate">判断节点是否满足条件的函数</param>
    /// <returns>满足条件的节点列表</returns>
    public List<T> FindAllNodes(T start, Func<T, IEnumerable<T>> getNeighbors, Func<T, bool> predicate)
    {
        List<T> result = new List<T>();
        
        if (start == null) return result;
        
        Queue<T> queue = new Queue<T>();
        HashSet<T> visited = new HashSet<T>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            
            if (predicate(current))
                result.Add(current);
                
            foreach (T neighbor in getNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return result;
    }
}