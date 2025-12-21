using System;
using UnityEngine;

[Obsolete("Use BagViewView's internal clear handling. This component is kept as a stub for compatibility.")]
public class ClearCardButton : MonoBehaviour
{
    // 已迁移到 BagViewView.OnCreate 中进行绑定，这里保持空实现以避免旧 prefab 报错
    void Start() { }
}
