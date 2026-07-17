using System;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Action<LayerMask, Collider> TriggerEnter;
    public Action<LayerMask, Collider> TriggerExit;
    public Action IDisable;

    public Collider colider;
    private GameObject owner;

    private void Awake()
    {
        owner = transform.root.gameObject;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if(collision.gameObject == owner) return;
        if (collision.transform.IsChildOf(owner.transform)) return;

        TriggerEnter?.Invoke(collision.gameObject.layer, collision);
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject == owner) return;
        if (collision.transform.IsChildOf(owner.transform)) return;

        TriggerExit?.Invoke(collision.gameObject.layer, collision);
    }
    private void OnDisable()
    {
        IDisable?.Invoke();
    }
}
