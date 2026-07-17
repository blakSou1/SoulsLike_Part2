using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MotionWarpingSystem : MonoBehaviour
{
    private MotionWarpSettings currentSettings;
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

    public void ApplyWarpSettings(MotionWarpSettings settings)
    {
        currentSettings = settings;
        startPosition = EntityTransform.position;
        startRotation = EntityTransform.rotation;
    }

    public void StartWarpingAnimationEvent(float duration)
    {
#if UNITY_EDITOR
        if (duration <= 0)
          throw new System.ArgumentException("Длительность варпа должна быть положительной");
#endif
          
        warpStartTime = Time.time;
        warpDuration = duration;
        isWarping = true;
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

        if (currentSettings.warpRotation)
            HandleRotation(rotationProgress);
        if (currentSettings.warpPosition)
            HandlePosition(positionProgress);
    }

    private void HandleRotation(float progress)
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
    
    private void HandlePosition(float progress)
    {
        Vector3 targetPos = GetTargetPosition();
        Vector3 entityPos = GetEntityPosition();

        Vector3 currentDirection = (targetPos - entityPos).normalized;
        currentDirection.y = 0;
        currentDirection.Normalize();

        float currentDistance = Vector3.Distance(entityPos, targetPos);

        Vector3 desiredPosition = targetPos - currentDirection * currentSettings.desiredDistance;

        if (currentSettings.ignoreVertical)
            desiredPosition.y = EntityTransform.position.y;

        float maxMoveDistance = currentSettings.maxWarpDistance;
        float moveDistance = Mathf.Min(currentDistance - currentSettings.desiredDistance, maxMoveDistance);

        if (currentDistance > currentSettings.maxWarpDistance * 1.5f)
        {
            desiredPosition = Vector3.MoveTowards(
                entityPos,
                targetPos - currentDirection * currentSettings.desiredDistance,
                currentSettings.maxWarpDistance * progress
            );

            if (currentSettings.ignoreVertical)
                desiredPosition.y = EntityTransform.position.y;
        }

        Vector3 newPosition = Vector3.Lerp(startPosition, desiredPosition, progress);

        if (currentSettings.ignoreVertical)
            newPosition.y = EntityTransform.position.y;

        if (progress > 0.9f)
        {
            float finalProgress = (progress - 0.9f) / 0.1f;
            newPosition = Vector3.Lerp(newPosition, desiredPosition, finalProgress);

            if (currentSettings.ignoreVertical)
                newPosition.y = EntityTransform.position.y;
        }

        EntityTransform.position = newPosition;
    }

    private Vector3 GetTargetPosition()
    {
        if (currentTarget == null) return Vector3.zero;

        Vector3 pos = currentTarget.position;
        if (currentSettings != null && currentSettings.ignoreVertical)
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
        if (!isWarping || currentTarget == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, currentTarget.position);

        Gizmos.DrawWireSphere(currentTarget.position, 0.2f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentSettings.maxWarpDistance);

        if (currentSettings.desiredDistance > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTarget.position, currentSettings.desiredDistance);
        }
    }
}