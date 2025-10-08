using UnityEngine;

[System.Serializable]
public class MotionWarpSettings
{
    public bool warpPosition = true;
    public bool warpRotation = true;
    public bool ignoreVertical = false;
    public float desiredDistance = 0f;

    [Header("Ограничения")]
    public float maxWarpDistance = 3f;
}

[RequireComponent(typeof(Animator))]
public class MotionWarpingSystem : MonoBehaviour
{
    private MotionWarpSettings currentSettings;
    public Transform currentTarget;
    public Transform EntityTransform;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isWarping;

    private float warpDuration;
    private float warpStartTime;

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

        float progress = Mathf.Clamp01((Time.time - warpStartTime) / warpDuration);

        if (currentSettings.warpRotation)
            HandleRotation(progress);
        if (currentSettings.warpPosition)
            HandlePosition(progress);
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
        Vector3 direction = (currentTarget.position - EntityTransform.position).normalized;
        Vector3 targetPosition = currentTarget.position - direction * currentSettings.desiredDistance;

        float distanceToTarget = Vector3.Distance(EntityTransform.position, targetPosition);
        float clampedDistance = Mathf.Min(distanceToTarget, currentSettings.maxWarpDistance);
        
        Vector3 limitedTargetPosition = EntityTransform.position + direction * clampedDistance;

        Vector3 newPosition = Vector3.Lerp(
            startPosition, 
            limitedTargetPosition, 
            progress
        );

        if (currentSettings.ignoreVertical)
            newPosition.y = EntityTransform.position.y;

        EntityTransform.position = newPosition;
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