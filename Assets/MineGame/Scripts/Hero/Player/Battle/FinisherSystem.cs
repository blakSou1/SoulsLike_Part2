using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinisherSystem : MonoBehaviour
{
    public string stateName = "Finisher";

    [Header("Finisher Settings")]
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float horizontalFOV = 90f;
    [SerializeField] private float verticalFOV = 60f;
    [SerializeField] private float finisherAngle = 120f;

    [Header("Timing")]
    [SerializeField] private float tickInterval = 0.2f;

    private List<Enemy> KnockedOutEnemies = new List<Enemy>();
    private Enemy targetKnockedOutEnemy = null;
    private Coroutine tickCoroutine;

    #region Public Methods

    /// <summary>
    /// Добавляет врага в список для добивания
    /// </summary>
    public void Knockout(Enemy enemy)
    {
        if (enemy != null && !KnockedOutEnemies.Contains(enemy))
            KnockedOutEnemies.Add(enemy);
    }

    /// <summary>
    /// Удаляет врага из списка при смерти
    /// </summary>
    public void IsDead(Enemy enemy)
    {
        if (KnockedOutEnemies.Contains(enemy))
        {
            KnockedOutEnemies.Remove(enemy);

            if (targetKnockedOutEnemy == enemy)
                targetKnockedOutEnemy = null;
        }
    }

    /// <summary>
    /// Проверяет, доступно ли добивание
    /// </summary>
    public bool IsFinisherAvailable()
    {
        return targetKnockedOutEnemy != null;
    }

    /// <summary>
    /// Получает текущую цель для добивания
    /// </summary>
    public Enemy GetFinisherTarget()
    {
        return targetKnockedOutEnemy;
    }

    /// <summary>
    /// Запускает добивание (вызывается извне)
    /// </summary>
    public bool StartFinisher()
    {
        if (targetKnockedOutEnemy == null)
            return false;

        AnimatorOverrideController clip = targetKnockedOutEnemy.animationFinished.GetAnimation();

        //ItemActionContainerModel actionContainer = null;

        G.playerView.AnimHook.Anim.runtimeAnimatorController = clip;

        G.playerView.AnimHook.Anim.Rebind();
        G.playerView.AnimHook.Anim.Update(0f);

        G.playerView.AnimHook.PlayTargetAnimation(stateName, true);

        targetKnockedOutEnemy.animationFinished.StartAnimEnd();
        // TODO: Запустить анимацию добивания
        // TODO: Эффекты

        KnockedOutEnemies.Remove(targetKnockedOutEnemy);
        targetKnockedOutEnemy = null;

        return true;
    }

    #endregion

    #region Unity Lifecycle

    private void FixedUpdate()
    {
        if (KnockedOutEnemies.Count > 0)
        {
            if (tickCoroutine == null)
                tickCoroutine = StartCoroutine(TickCoroutine());
        }
        else
        {
            if (tickCoroutine != null)
            {
                StopCoroutine(tickCoroutine);
                tickCoroutine = null;
                targetKnockedOutEnemy = null;
            }
        }
    }

    #endregion

    #region Core Logic

    private IEnumerator TickCoroutine()
    {
        while (KnockedOutEnemies.Count > 0)
        {
            yield return new WaitForSeconds(tickInterval);

            Enemy bestTarget = SelectBestTarget();

            if (bestTarget != targetKnockedOutEnemy)
                targetKnockedOutEnemy = bestTarget;
        }

        tickCoroutine = null;
    }

    /// <summary>
    /// Выбирает лучшую цель для добивания
    /// </summary>
    private Enemy SelectBestTarget()
    {
        if (G.playerView.LockOnComponent.CurrentLockable != null)
        {
            Enemy lockedEnemy = G.playerView.LockOnComponent.CurrentLockable as Enemy;

            if (lockedEnemy != null &&
                KnockedOutEnemies.Contains(lockedEnemy) &&
                CanPerformFinisherOn(lockedEnemy))
            {
                float distance = Vector3.Distance(transform.position, lockedEnemy.transform.position);
                if (distance <= maxDistance)
                {
                    Vector3 directionToTarget = (lockedEnemy.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, directionToTarget);

                    if (angle <= finisherAngle / 2f)
                        return lockedEnemy;
                }
            }
        }

        Enemy bestTarget = null;
        float maxScore = -Mathf.Infinity;

        foreach (Enemy enemy in KnockedOutEnemies)
        {
            if (!CanPerformFinisherOn(enemy))
                continue;

            if (!IsVisible(enemy))
                continue;

            float score = CalculateScore(enemy);

            if (score > maxScore)
            {
                maxScore = score;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Проверяет, можно ли выполнить добивание на враге
    /// </summary>
    private bool CanPerformFinisherOn(Enemy enemy)
    {
        if (enemy == null) return false;

        if (!KnockedOutEnemies.Contains(enemy)) return false;

        return true;
    }

    /// <summary>
    /// Рассчитывает скор для врага
    /// </summary>
    private float CalculateScore(Enemy enemy)
    {
        float distanceScore = 1 - Mathf.Clamp01(
            Vector3.Distance(G.playerView.transform.position, enemy.GetLockOnTarget().position) / maxDistance
        );

        float angleScore = GetAnglePriority(enemy);

        // Веса для параметров
        float finalScore = angleScore * 0.6f + distanceScore * 0.4f;

        return finalScore;
    }

    /// <summary>
    /// Приоритет по углу (0-1)
    /// </summary>
    private float GetAnglePriority(Enemy enemy)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(enemy.transform.position);
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        float distance = Vector3.Distance(screenPos, screenCenter);
        return 1 - (distance / (Screen.width * 0.5f));
    }

    /// <summary>
    /// Проверяет, видим ли враг камерой
    /// </summary>
    private bool IsVisible(Enemy target)
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

        float distance = Vector3.Distance(G.playerView.transform.position, targetPos);
        if (distance > maxDistance) return false;

        if (Physics.Raycast(cameraPos, dirToTarget, out RaycastHit hit, distance))
        {
            if (!hit.collider.CompareTag("Enemy"))
                return false;
        }

        return true;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        if (targetKnockedOutEnemy != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetKnockedOutEnemy.transform.position);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetKnockedOutEnemy.transform.position, 0.5f);
        }
    }

    #endregion
}
