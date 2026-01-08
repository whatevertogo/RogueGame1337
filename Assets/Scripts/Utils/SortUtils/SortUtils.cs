public static class SortUtils
{
    /// <summary>
    /// 对整数数组进行冒泡排序（优化版）
    /// </summary>
    public static void BubbleSort(int[] array)
    {
        int n = array.Length;
        bool swapped;

        for (int i = 0; i < n - 1; i++)
        {
            swapped = false;
            // n-i-1 是因为每一轮最大的数都会沉底，不需要重复比较
            for (int j = 0; j < n - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    // 交换元素
                    int temp = array[j];
                    array[j] = array[j + 1];
                    array[j + 1] = temp;
                    swapped = true;
                }
            }

            // 如果这一轮没有发生交换，说明已经排好序了，直接跳出
            if (!swapped)
            {
                break;
            }
        }
    }
}
