using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MotionWarpingSystem : MonoBehaviour
{
    private ItemActionContainerModel currentSettings;
    [ReadOnly] public Transform currentTarget;
    public Transform EntityTransform;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isWarping;

    private float warpDuration;
    private float warpStartTime;

    [Header("Warp Settings")]
    [SerializeField] private AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public void ApplyWarpSettings(ItemActionContainerModel settings) =>
        currentSettings = settings;

    public void StartWarpingAnimationEvent(float duration)
    {
#if UNITY_EDITOR
        if (duration <= 0)
          throw new System.ArgumentException("Длительность варпа должна быть положительной");
#endif

        if (!CanStartWarp())
            return;

        startPosition = EntityTransform.position;
        startRotation = EntityTransform.rotation;

        warpStartTime = Time.time;
        warpDuration = duration;
        isWarping = true;
    }

    public bool CanStartWarp()
    {
        if (currentTarget == null || currentSettings == null)
            return false;

        float distance = Vector3.Distance(EntityTransform.position, currentTarget.position);
        bool canWarp = distance <= currentSettings.warpSettings.activationDistance;

        return canWarp;
    }

    public void StopWarpingAnimationEvent()
    {
        isWarping = false;
    }

    private void OnAnimatorMove()
    {
        if (!isWarping || currentTarget == null || currentSettings == null) return;

        float rawProgress = (Time.time - warpStartTime) / warpDuration;
        float progress = Mathf.Clamp01(rawProgress);

        float positionProgress = positionCurve.Evaluate(progress);
        float rotationProgress = rotationCurve.Evaluate(progress);

        if (currentSettings.warpSettings.warpRotation)
            HandleRotation(rotationProgress);
        if (currentSettings.warpSettings.warpPosition)
            HandlePosition(positionProgress);
    }

    private void HandleRotation(float progress)
    {
        if (currentSettings.warpSettings.snapToFinish)
        {
            Vector3 directionToTarget = (currentTarget.position - EntityTransform.position).normalized;
            directionToTarget.y = 0;

            if (directionToTarget == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            targetRotation *= Quaternion.Euler(0, currentSettings.warpSettings.targetRotationOffset, 0);

            EntityTransform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                progress
            );
            Debug.Log($"[Warp] Applying offset: {currentSettings.warpSettings.targetRotationOffset}, progress: {progress}");
        }
        else
        {
            Vector3 lookDirection = (currentTarget.position - EntityTransform.position).normalized;
            lookDirection.y = 0;

            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                EntityTransform.rotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    progress
                );
            }
        }
    }

    private void HandlePosition(float progress)
    {
        Vector3 targetPos = GetTargetPosition();
        Vector3 entityPos = GetEntityPosition();

        Vector3 currentDirection = (targetPos - entityPos).normalized;
        currentDirection.y = 0;
        currentDirection.Normalize();

        float currentDistance = Vector3.Distance(entityPos, targetPos);

        Vector3 desiredPosition = targetPos - currentDirection * currentSettings.warpSettings.desiredDistance;

        if (currentSettings.warpSettings.ignoreVertical)
            desiredPosition.y = EntityTransform.position.y;

        float maxMoveDistance = currentSettings.warpSettings.maxWarpDistance;
        float moveDistance = Mathf.Min(currentDistance - currentSettings.warpSettings.desiredDistance, maxMoveDistance);

        if (currentDistance > currentSettings.warpSettings.maxWarpDistance * 1.5f)
        {
            desiredPosition = Vector3.MoveTowards(
                entityPos,
                targetPos - currentDirection * currentSettings.warpSettings.desiredDistance,
                currentSettings.warpSettings.maxWarpDistance * progress
            );

            if (currentSettings.warpSettings.ignoreVertical)
                desiredPosition.y = EntityTransform.position.y;
        }

        Vector3 newPosition = Vector3.Lerp(startPosition, desiredPosition, progress);

        if (currentSettings.warpSettings.ignoreVertical)
            newPosition.y = EntityTransform.position.y;

        if (progress > 0.9f)
        {
            float finalProgress = (progress - 0.9f) / 0.1f;
            newPosition = Vector3.Lerp(newPosition, desiredPosition, finalProgress);

            if (currentSettings.warpSettings.ignoreVertical)
                newPosition.y = EntityTransform.position.y;
        }

        EntityTransform.position = newPosition;
    }

    private Vector3 GetTargetPosition()
    {
        if (currentTarget == null) return Vector3.zero;

        Vector3 pos = currentTarget.position;
        if (currentSettings != null && currentSettings.warpSettings.ignoreVertical)
            pos.y = EntityTransform.position.y;
        return pos;
    }

    private Vector3 GetEntityPosition()
    {
        Vector3 pos = EntityTransform.position;
        return pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isWarping || currentTarget == null || currentSettings == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(EntityTransform.position, currentSettings.warpSettings.activationDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(EntityTransform.position, currentSettings.warpSettings.maxWarpDistance);

        Vector3 targetPos = GetTargetPosition();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, 0.2f);

        Gizmos.color = Color.cyan;
        Vector3 direction = (targetPos - EntityTransform.position).normalized;
        direction.y = 0;
        if (direction.magnitude > 0.01f)
        {
            Quaternion offsetRot = Quaternion.Euler(0, currentSettings.warpSettings.targetRotationOffset, 0);
            Vector3 rotatedDir = offsetRot * direction;
            Gizmos.DrawRay(EntityTransform.position, rotatedDir * 2f);
        }
    }

}