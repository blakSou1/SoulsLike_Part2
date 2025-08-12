using UnityEngine;

public class Enemy : MonoBehaviour, ILockable
{
    public Transform GetLockOnTarget(Transform from)
    {
        return transform;
    }

    public bool IsAlive()
    {
        throw new System.NotImplementedException();
    }

}
