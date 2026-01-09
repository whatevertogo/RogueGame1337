using System;
using System.Collections;
using CDTU.Utils;
using Character.Interfaces;
using RogueGame.Map;
using UnityEngine;

/// <summary>
/// 过渡控制器：负责房间切换时的玩家传送和相机移动
/// 核心职责：
/// 1. 锁定/解锁玩家移动
/// 2. 传送玩家到目标位置
/// 3. 平滑移动相机到目标位置
/// 4. 协调完整的房间切换流程
/// </summary>
public sealed class TransitionController : MonoBehaviour
{
    [Header("过渡设置")]
    [Tooltip("过渡完成后等待的缓冲时间（秒）")]
    [SerializeField]
    private float _postSwitchDelay = 0.25f;

    [Header("摄像机")]
    [Tooltip("主相机引用（留空则使用 Camera.main）")]
    [SerializeField]
    private Camera _mainCamera;

    [Tooltip("相机移动时长（秒）")]
    [SerializeField]
    private float _cameraSwitchDuration = 0.5f;

    [Header("传送设置")]
    [Tooltip("传送后禁用玩家移动的时长（秒）")]
    [SerializeField]
    private float _teleportMovementDisableTime = 0.15f;

    private PlayerManager _playerManager;

    // ═══════════════════════════════════════════════════════════
    // 初始化
    // ═══════════════════════════════════════════════════════════

    public void Initialize(PlayerManager playerManager)
    {
        _playerManager = playerManager;
    }

    // ═══════════════════════════════════════════════════════════
    // 玩家移动控制
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 锁定所有玩家的移动
    /// </summary>
    public void LockAllPlayersMovement()
    {
        if (_playerManager == null)
            return;

        foreach (var playerData in _playerManager.GetAllPlayersData())
        {
            if (playerData?.Controller == null)
                continue;

            var movement = playerData.Controller.GetComponent<IPlayerMovement>();
            movement?.SetCanMove(false);
        }
    }

    /// <summary>
    /// 解锁所有玩家的移动
    /// </summary>
    public void UnlockAllPlayersMovement()
    {
        if (_playerManager == null)
            return;

        foreach (var playerData in _playerManager.GetAllPlayersData())
        {
            if (playerData?.Controller == null)
                continue;

            var movement = playerData.Controller.GetComponent<IPlayerMovement>();
            movement?.SetCanMove(true);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 传送逻辑
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 传送玩家到目标房间，并短暂禁用移动
    /// </summary>
    public void TeleportPlayer(RoomPrefab room, Direction entryDir)
    {
        if (room == null)
        {
            CDLogger.LogWarning("[TransitionController] 房间为 null，无法传送");
            return;
        }

        if (_playerManager == null)
        {
            CDLogger.LogWarning("[TransitionController] PlayerManager 未初始化，无法传送");
            return;
        }

        Vector3 targetPos = GetTargetPosition(room, entryDir);

        foreach (var playerData in _playerManager.GetAllPlayersData())
        {
            if (playerData?.Controller == null)
                continue;

            TeleportSinglePlayer(playerData.Controller.gameObject, targetPos);
        }
    }

    /// <summary>
    /// 获取目标传送位置
    /// </summary>
    private Vector3 GetTargetPosition(RoomPrefab room, Direction entryDir)
    {
        if (entryDir != Direction.None)
        {
            return room.GetExitPosition(entryDir);
        }

        return room.PlayerSpawn != null ? room.PlayerSpawn.position : room.transform.position;
    }

    /// <summary>
    /// 传送单个玩家
    /// </summary>
    private void TeleportSinglePlayer(GameObject player, Vector3 targetPos)
    {
        var transform = player.transform;
        var rb = player.GetComponent<Rigidbody2D>();
        var movement = player.GetComponent<IPlayerMovement>();

        // 保持 Z 轴位置
        targetPos.z = transform.position.z;

        CDLogger.Log($"[TransitionController] 传送玩家: {transform.position} -> {targetPos}");

        // 停止移动并禁用控制
        movement?.StopImmediately();
        movement?.SetCanMove(false);

        // 传送（优先使用 Rigidbody）
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.position = targetPos;
        }
        else
        {
            transform.position = targetPos;
        }

        // 延迟恢复移动
        if (movement != null)
        {
            StartCoroutine(ReenableMovementAfterDelay(_teleportMovementDisableTime, movement));
        }
    }

    /// <summary>
    /// 延迟恢复玩家移动
    /// </summary>
    private IEnumerator ReenableMovementAfterDelay(float delay, IPlayerMovement movement)
    {
        yield return new WaitForSeconds(delay);
        movement?.SetCanMove(true);
    }

    // ═══════════════════════════════════════════════════════════
    // 相机移动
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 平滑移动相机到目标位置
    /// </summary>
    public void MoveCameraTo(Vector3 targetPos)
    {
        Camera cam = _mainCamera != null ? _mainCamera : Camera.main;

        if (cam == null)
        {
            CDLogger.LogWarning("[TransitionController] 找不到相机");
            return;
        }

        StartCoroutine(MoveCameraCoroutine(cam, targetPos));
    }

    private IEnumerator MoveCameraCoroutine(Camera cam, Vector3 targetPos)
    {
        Vector3 startPos = cam.transform.position;
        targetPos.z = startPos.z;

        float elapsed = 0f;
        while (elapsed < _cameraSwitchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / _cameraSwitchDuration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        cam.transform.position = targetPos;
    }

    // ═══════════════════════════════════════════════════════════
    // 房间切换协程
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 执行完整的房间切换流程
    /// 1. 锁定玩家移动
    /// 2. 执行切换动作（switchAction）
    /// 3. 等待缓冲时间
    /// 4. 解锁玩家移动
    /// </summary>
    public IEnumerator DoRoomTransitionCoroutine(Action switchAction)
    {
        LockAllPlayersMovement();
        yield return null; // 等待一帧确保输入被禁用

        switchAction?.Invoke();

        yield return new WaitForSeconds(_postSwitchDelay);

        UnlockAllPlayersMovement();
    }
}
