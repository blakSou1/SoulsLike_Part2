using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public class LockOnComponent : MonoBehaviour, IDisposable
{
    [SerializeField] private float maxLockDistance = 15f;
    [Header("Field of View")]
    [SerializeField] private float horizontalFOV = 90f;
    [SerializeField] private float verticalFOV = 60f;
    [ReadOnly] public bool lockOn = false;

    private List<ILockable> _potentialTargets = new();
    public ILockable CurrentLockable { get; private set; }

    [SerializeField] private LayerMask LockOnMask = 1 << 3;
    private CinemachineCamera FollowCinemachine;
    private CinemachineFollow Follow;

    #region Life
    private void Start()
    {
        FollowCinemachine = FindFirstObjectByType<CinemachineFollow>(FindObjectsInactive.Include).GetComponent<CinemachineCamera>();
        Follow = FollowCinemachine.GetComponent<CinemachineFollow>();

        G.inputBuffer.LockOn += LockOn;
        G.inputBuffer.SwitchTarget += SwitchTarget;
    }

    public void Dispose()
    {
        G.inputBuffer.LockOn -= LockOn;
        G.inputBuffer.SwitchTarget -= SwitchTarget;
    }
    #endregion

    public void OnDrawGizmos()
    {
        if (!Application.isPlaying || Camera.main == null) return;

        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraUp = Camera.main.transform.up;
        Vector3 cameraRight = Camera.main.transform.right;

        float halfHorizontal = horizontalFOV * 0.5f * Mathf.Deg2Rad;
        Vector3 leftDir = Quaternion.Euler(0, -halfHorizontal * Mathf.Rad2Deg, 0) * cameraForward;
        Vector3 rightDir = Quaternion.Euler(0, halfHorizontal * Mathf.Rad2Deg, 0) * cameraForward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(cameraPos, leftDir * maxLockDistance);
        Gizmos.DrawRay(cameraPos, rightDir * maxLockDistance);
        Gizmos.DrawRay(cameraPos, cameraForward * maxLockDistance);

        float halfVertical = verticalFOV * 0.5f * Mathf.Deg2Rad;
        Vector3 upDir = Quaternion.Euler(halfVertical * Mathf.Rad2Deg, 0, 0) * cameraForward;
        Vector3 downDir = Quaternion.Euler(-halfVertical * Mathf.Rad2Deg, 0, 0) * cameraForward;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(cameraPos, upDir * maxLockDistance);
        Gizmos.DrawRay(cameraPos, downDir * maxLockDistance);

        foreach (var target in _potentialTargets)
        {
            Vector3 targetPos = target.GetLockOnTarget().position;
            bool visible = IsVisible(target);

            Gizmos.color = visible ? Color.green : Color.red;
            Gizmos.DrawWireSphere(targetPos, 0.5f);

            if (visible)
            {
                Gizmos.DrawLine(cameraPos, targetPos);
            }
        }
    }
    private void LockOn()
    {
        if (lockOn)
            DisableLockOn();
        else
        {
            FindLockableTarget();
            CurrentLockable = SelecktBesttarget();

            if (CurrentLockable != null)
            {
                G.playerView.motionWarpingSystem.currentTarget = CurrentLockable.GetLockOnTarget();
                Follow.TrackerSettings.RotationDamping = new(0, 0, 0);

                FollowCinemachine.LookAt = CurrentLockable.GetLockOnTarget();
                lockOn = true;
            }
        }
        FollowCinemachine.gameObject.SetActive(lockOn);
    }

    private void DisableLockOn()
    {        
        Follow.TrackerSettings.RotationDamping = new(100, 100, 100);
        FollowCinemachine.LookAt = null;
        lockOn = false;
        CurrentLockable = null;
        G.playerView.motionWarpingSystem.currentTarget = null;
    }
    
    private void FindLockableTarget()
    {
        _potentialTargets.Clear();
        Collider[] hits = Physics.OverlapSphere(G.playerView.transform.position, maxLockDistance, LockOnMask);
        foreach (Collider hit in hits)
            if (hit.TryGetComponent(out ILockable target))
                _potentialTargets.Add(target);
    }

    #region Sub
    private ILockable SelecktBesttarget()
    {
        ILockable bestTarget = null;
        float maxScore = -Mathf.Infinity;

        foreach (ILockable target in _potentialTargets)
        {
            if (!IsVisible(target)) continue;

            float distanceScore = 1 - Mathf.Clamp01(
                Vector3.Distance(G.playerView.transform.position, target.GetLockOnTarget().position) / maxLockDistance
            );

            float finalScore =
                GetAnglePriority(target) * 0.5f
                + distanceScore * 0.3f;

            if (finalScore > maxScore)
            {
                maxScore = finalScore;
                bestTarget = target;
            }
        }
        return bestTarget;
    }
    
    private float GetAnglePriority(ILockable enemy)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(enemy.GetLockOnTarget().position);
        Vector3 screenCenter = new(Screen.width / 2, Screen.height / 2, 0);
        return 1 - (Vector3.Distance(screenPos, screenCenter) / Screen.width);
    }

    private bool IsVisible(ILockable target)
    {
        if (Camera.main == null) return false;

        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraUp = Camera.main.transform.up;
        Vector3 cameraRight = Camera.main.transform.right;
        Vector3 targetPos = target.GetLockOnTarget().position;

        Vector3 dirToTarget = (targetPos - cameraPos).normalized;

        float dot = Vector3.Dot(cameraForward, dirToTarget);
        if (dot < 0) return false;

        Vector3 horizontalDir = Vector3.ProjectOnPlane(dirToTarget, cameraUp).normalized;
        float horizontalAngle = Vector3.Angle(
            Vector3.ProjectOnPlane(cameraForward, cameraUp).normalized,
            horizontalDir
        );

        if (horizontalAngle > horizontalFOV * 0.5f) return false;

        Vector3 verticalDir = Vector3.ProjectOnPlane(dirToTarget, cameraRight).normalized;
        float verticalAngle = Vector3.Angle(
            Vector3.ProjectOnPlane(cameraForward, cameraRight).normalized,
            verticalDir
        );

        if (verticalAngle > verticalFOV * 0.5f) return false;

        float distance = Vector3.Distance(cameraPos, targetPos);
        if (distance > maxLockDistance) return false;

        return !Physics.Raycast(cameraPos, dirToTarget, distance, ~LockOnMask);
    }

    private void SwitchTarget(int direction)
    {
        if (!lockOn) return;
        if (_potentialTargets.Count == 0 || CurrentLockable == null) return;

        // Получаем текущую позицию блокировки
        Vector3 currentLockPosition = CurrentLockable.GetLockOnTarget().position;

        // Вычисляем горизонтальный угол текущей цели
        float currentAngle = GetHorizontalAngle(currentLockPosition);

        // Фильтруем и сортируем потенциальные цели
        var targetsWithAngles = _potentialTargets
            .Where(t => t != CurrentLockable) // Исключаем текущую цель
            .Select(t => new
            {
                Target = t,
                Position = t.GetLockOnTarget().position,
                Angle = GetHorizontalAngle(t.GetLockOnTarget().position)
            })
            .Where(t =>
                // Выбираем цели в нужном направлении
                (direction > 0 && t.Angle > currentAngle) ||
                (direction < 0 && t.Angle < currentAngle)
            )
            .OrderBy(t => Mathf.Abs(t.Angle - currentAngle))
            .ToList();

        // Выбираем ближайшую цель
        if (targetsWithAngles.Count > 0)
        {
            var nearestTarget = targetsWithAngles.First().Target;

            // Обновляем текущую цель и камеру
            CurrentLockable = nearestTarget;
            G.playerView.motionWarpingSystem.currentTarget = CurrentLockable.GetLockOnTarget();

            FollowCinemachine.LookAt = CurrentLockable.GetLockOnTarget();
        }
    }
    
    private float GetHorizontalAngle(Vector3 targetPos)
    {
        Vector3 dirToTarget = targetPos - G.playerView.transform.position;
        Vector3 flatDirToTarget = new Vector3(dirToTarget.x, 0, dirToTarget.z).normalized;

        // Используем SignedAngle для правильного определения направления
        return Vector3.SignedAngle(G.playerView.transform.forward, flatDirToTarget, Vector3.up);
    }
    #endregion

}