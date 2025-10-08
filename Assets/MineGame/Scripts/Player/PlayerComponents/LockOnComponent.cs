using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public class LockOnComponent : IDisposable
{
    [SerializeField] private float maxLockDistance = 15f;
    [SerializeField] private float fieldOfView = 60f;
    [ReadOnly] public bool lockOn = false;

    private List<ILockable> _potentialTargets = new();
    public ILockable currentLockable { get; private set; }

    [SerializeField] private LayerMask LockOnMask = 1 << 3;
    private CinemachineCamera FollowCinemachine;
    private CinemachineFollow Follow;

    private PlayerView _playerView;

    #region Life
    public void Init(PlayerView playerView)
    {
        _playerView = playerView;
        
        FollowCinemachine = GameObject.FindFirstObjectByType<CinemachineFollow>(FindObjectsInactive.Include).GetComponent<CinemachineCamera>();
        Follow = FollowCinemachine.GetComponent<CinemachineFollow>();

        G.input.Player.LockOn.started += i => LockOn();
        G.input.Player.NewTargetLock.started += i =>
        {
            float inputValue = i.ReadValue<float>();
            int deadZone = 20;

            if (Mathf.Abs(inputValue) > deadZone)
                SwitchTarget(inputValue > 0 ? 1 : -1);
        };

    }

    public void Dispose()
    {
        G.input.Player.LockOn.started -= i => LockOn();
        G.input.Player.NewTargetLock.started -= i =>
        {
            float inputValue = i.ReadValue<float>();
            int deadZone = 20;

            if (Mathf.Abs(inputValue) > deadZone)
                SwitchTarget(inputValue > 0 ? 1 : -1);
        };
    }
    #endregion

    private void LockOn()
    {
        if (lockOn)
            DisableLockOn();
        else
        {
            FindLockableTarget();
            currentLockable = SelecktBesttarget();

            if (currentLockable != null)
            {
                Follow.TrackerSettings.RotationDamping = new(0, 0, 0);

                FollowCinemachine.LookAt = currentLockable.GetLockOnTarget(_playerView.transform);
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
        currentLockable = null;
    }
    private void FindLockableTarget()
    {
        _potentialTargets.Clear();
        Collider[] hits = Physics.OverlapSphere(_playerView.transform.position, maxLockDistance, LockOnMask);
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
                Vector3.Distance(_playerView.transform.position, target.GetLockOnTarget(_playerView.transform).position) / maxLockDistance
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
        Vector3 screenPos = Camera.main.WorldToScreenPoint(enemy.GetLockOnTarget(_playerView.transform).position);
        Vector3 screenCenter = new(Screen.width / 2, Screen.height / 2, 0);
        return 1 - (Vector3.Distance(screenPos, screenCenter) / Screen.width);
    }
    private bool IsVisible(ILockable target)
    {
        Vector3 dir = (target.GetLockOnTarget(_playerView.transform).position - _playerView.transform.position).normalized;
        float angle = Vector3.Angle(_playerView.transform.forward, dir);

        return angle <= fieldOfView * 0.5f
            && !Physics.Raycast(_playerView.transform.position, dir, out _, maxLockDistance, ~LockOnMask);
    }
    private void SwitchTarget(int direction)
    {
        if (!lockOn) return;
        if (_potentialTargets.Count == 0 || currentLockable == null) return;

        // Получаем текущую позицию блокировки
        Vector3 currentLockPosition = currentLockable.GetLockOnTarget(_playerView.transform).position;

        // Вычисляем горизонтальный угол текущей цели
        float currentAngle = GetHorizontalAngle(currentLockPosition);

        // Фильтруем и сортируем потенциальные цели
        var targetsWithAngles = _potentialTargets
            .Where(t => t != currentLockable) // Исключаем текущую цель
            .Select(t => new
            {
                Target = t,
                Position = t.GetLockOnTarget(_playerView.transform).position,
                Angle = GetHorizontalAngle(t.GetLockOnTarget(_playerView.transform).position)
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
            currentLockable = nearestTarget;
            FollowCinemachine.LookAt = currentLockable.GetLockOnTarget(_playerView.transform);
        }
    }
    private float GetHorizontalAngle(Vector3 targetPos)
    {
        Vector3 dirToTarget = targetPos - _playerView.transform.position;
        Vector3 flatDirToTarget = new Vector3(dirToTarget.x, 0, dirToTarget.z).normalized;

        // Используем SignedAngle для правильного определения направления
        return Vector3.SignedAngle(_playerView.transform.forward, flatDirToTarget, Vector3.up);
    }
    #endregion

}