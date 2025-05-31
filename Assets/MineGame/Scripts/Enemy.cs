using UnityEngine;

public class Enemy : MonoBehaviour, ILockable
{
    public Transform GetLockOnTarget(Transform from)
    {
        return transform;
    }

    public bool isAlive()
    {
        throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
