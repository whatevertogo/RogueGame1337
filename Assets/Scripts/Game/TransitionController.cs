using System;
using System.Collections;
using UnityEngine;
using RogueGame.Map;
using Character.Interfaces;

/// <summary>
/// 负责过渡期间的通用逻辑：锁定玩家移动、执行切换动作、解锁。
/// 这是一个轻量级实现，后续可以扩展相机淡入/淡出或动效。
/// </summary>
public sealed class TransitionController : MonoBehaviour
{
    [Tooltip("过渡完成后等待的缓冲时间（秒）")]
    public float postSwitchDelay = 0.25f;
    [Header("摄像机 / 传送 设置")]
    public Camera mainCamera;
    public float cameraSwitchDuration = 0.5f;
    public float teleportMovementDisableTime = 0.15f;

    /// <summary>
    /// 锁定所有已注册玩家的移动（通过 PlayerManager 中的 PlayerData.Controller 查找 IPlayerMovement）
    /// </summary>
    public void LockAllPlayersMovement()
    {
        var pm = PlayerManager.Instance;
        if (pm == null) return;
        foreach (var p in pm.GetAllPlayersData())
        {
            if (p?.Controller == null) continue;
            var mv = p.Controller.GetComponent<IPlayerMovement>();
            mv?.SetCanMove(false);
        }
    }

    public void UnlockAllPlayersMovement()
    {
        var pm = PlayerManager.Instance;
        if (pm == null) return;
        foreach (var p in pm.GetAllPlayersData())
        {
            if (p?.Controller == null) continue;
            var mv = p.Controller.GetComponent<IPlayerMovement>();
            mv?.SetCanMove(true);
        }
    }

    /// <summary>
    /// 立即传送玩家到房间的出生/出口位置，并短暂禁用玩家移动（可由 RoomManager 或 GameManager 调用）
    /// </summary>
    public void TeleportPlayer(RoomPrefab room, Direction entryDir)
    {
        if (room == null) return;
        var pm = PlayerManager.Instance;
        if (pm == null)
        {
            Debug.LogWarning("[TransitionController] 找不到 PlayerManager，无法传送玩家。");
            return;
        }

        Vector3 exitPos = entryDir != Direction.None ? room.GetExitPosition(entryDir) : (room.PlayerSpawn != null ? room.PlayerSpawn.position : room.transform.position);

        foreach (var p in pm.GetAllPlayersData())
        {
            if (p == null || p.Controller == null) continue;

            var playerGO = p.Controller.gameObject;
            var playerTransform = playerGO.transform;
            var playerRb = playerGO.GetComponent<Rigidbody2D>();
            var playerMovement = playerGO.GetComponent<IPlayerMovement>();

            Vector3 targetPos = exitPos;
            targetPos.z = playerTransform.position.z;

            Debug.Log($"[TransitionController] 传送玩家 {p.PlayerId}: {playerTransform.position} -> {targetPos}");

            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.position = targetPos;
            }
            else
            {
                playerTransform.position = targetPos;
            }

            if (playerMovement != null)
            {
                // 停止并暂时禁用移动，等待短延迟后恢复
                playerMovement.StopImmediately();
                playerMovement.SetCanMove(false);
                StartCoroutine(ReenableMovementAfterDelay(teleportMovementDisableTime, playerMovement));
            }
        }
    }

    private IEnumerator ReenableMovementAfterDelay(float delay, IPlayerMovement playerMovement)
    {
        yield return new WaitForSeconds(delay);
        playerMovement?.SetCanMove(true);
    }

    /// <summary>
    /// 将主相机平滑移动到目标位置
    /// </summary>
    public void MoveCameraTo(Vector3 targetPos)
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) return;
        StartCoroutine(MoveCameraCoroutine(cam, targetPos));
    }

    private IEnumerator MoveCameraCoroutine(Camera cam, Vector3 targetPos)
    {
        Vector3 startPos = cam.transform.position;
        targetPos.z = startPos.z;

        float elapsed = 0f;
        while (elapsed < cameraSwitchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / cameraSwitchDuration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        cam.transform.position = targetPos;
    }

    /// <summary>
    /// 协调一次房间切换：在切换前锁定玩家移动，调用 providedAction 执行实际切换（例如 RoomManager.SwitchToNextRoom），
    /// 切换后等待一段时间再解锁移动。
    /// </summary>
    public IEnumerator DoRoomTransitionCoroutine(Action switchAction)
    {
        LockAllPlayersMovement();
        // 等一帧以保证输入及时被禁用
        yield return null;

        try
        {
            switchAction?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"TransitionController: 切换动作执行异常: {ex}");
        }

        // 等待相机/传送相关协程有机会运行
        yield return new WaitForSeconds(postSwitchDelay);

        UnlockAllPlayersMovement();
    }
}
