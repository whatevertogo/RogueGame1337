using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// 与该物体交互
    /// </summary>
    /// <param name="interactor"></param>
    void Interact(GameObject interactor);

    /// <summary>
    /// 当玩家离开交互范围时调用
    /// </summary>
    void OnPlayerEnter(GameObject interactor);

    /// <summary>
    /// 当玩家离开交互范围时调用
    /// </summary>
    void OnPlayerExit(GameObject interactor);
}
