using UnityEngine;

/// <summary>
/// обект с этим скриптом всегда следит за указаным обьектом по указаным осям, чтобы остановить слежение сбросьте трансформ таргет.
/// </summary>
[ExecuteAlways]
public class LookAtTarget : MonoBehaviour
{
    [Tooltip("Target to look at.")]
    public Transform Target;

    [Tooltip("Lock rotation along the x axis to the initial value.")]
    public bool LockRotationX;
    [Tooltip("Lock rotation along the y axis to the initial value.")]
    public bool LockRotationY;
    [Tooltip("Lock rotation along the z axis to the initial value.")]
    public bool LockRotationZ;
    
    private Vector3 m_Rotation;
    
    private void OnEnable()
    {
        m_Rotation = transform.rotation.eulerAngles;
    }

    public void Reset()
    {
        m_Rotation = transform.rotation.eulerAngles;
    }

    private void FixedUpdate()
    {
        if (Target != null)
        {
            var direction = Target.position - transform.position;
            transform.rotation = Quaternion.LookRotation(direction);

            if (LockRotationX || LockRotationY || LockRotationZ)
            {
                var euler = transform.rotation.eulerAngles;
                euler.x = LockRotationX ? m_Rotation.x : euler.x;
                euler.y = LockRotationY ? m_Rotation.y : euler.y;
                euler.z = LockRotationZ ? m_Rotation.z : euler.z;
                transform.rotation = Quaternion.Euler(euler);
            }
        }
    }
}
