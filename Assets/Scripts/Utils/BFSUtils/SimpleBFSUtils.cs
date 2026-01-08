using System;
using System.Collections.Generic;

namespace CDTU.Utils
{
    /// <summary>
    /// 简化版的BFS（广度优先搜索）工具类
    /// </summary>
    /// <typeparam name="T">节点类型</typeparam>
    public class SimpleBFSUtil<T>
    {
        /// <summary>
        /// 执行基本的BFS遍历
        /// </summary>
        /// <param name="start">起始节点</param>
        /// <param name="getNeighbors">获取邻接节点的函数</param>
        /// <param name="process">处理节点的函数</param>
        public void BFS(T start, Func<T, IEnumerable<T>> getNeighbors, Action<T> process)
        {
            var queue = new Queue<T>();
            var visited = new HashSet<T>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                T current = queue.Dequeue();
                process(current);

                foreach (var neighbor in getNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// 寻找从起点到终点的最短路径
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="getNeighbors">获取邻接节点的函数</param>
        /// <returns>最短路径，如果不存在则返回空列表</returns>
        public List<T> FindShortestPath(T start, T end, Func<T, IEnumerable<T>> getNeighbors)
        {
            var queue = new Queue<T>();
            var visited = new HashSet<T>();
            var parentMap = new Dictionary<T, T>();

            queue.Enqueue(start);
            visited.Add(start);

            bool found = false;
            while (queue.Count > 0 && !found)
            {
                var current = queue.Dequeue();

                foreach (var neighbor in getNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                        parentMap[neighbor] = current;

                        if (EqualityComparer<T>.Default.Equals(neighbor, end))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found) return new List<T>();

            // 重建路径
            var path = new List<T>();
            var currentNode = end;

            while (true)
            {
                path.Add(currentNode);
                if (EqualityComparer<T>.Default.Equals(currentNode, start))
                    break;
                currentNode = parentMap[currentNode];
            }

            path.Reverse();
            return path;
        }
    }
}