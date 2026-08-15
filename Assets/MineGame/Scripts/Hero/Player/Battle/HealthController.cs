using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    [NonSerialized] public Weapon weapon;

    private AnimatorHookView anim;

    private List<Collider> EnterObject = new();

    public void Start()
    {
        anim = GetComponentInChildren<AnimatorHookView>();

        weapon.TriggerEnter += Enter;
        weapon.TriggerExit += Exit;
        weapon.IDisable += Disable;
        anim.IOpenDamageCollider += Open;
        anim.ICloseDamageCollider += Close;

        weapon.collider.enabled = false;
    }

    public void OnDestroy()
    {
        weapon.TriggerEnter -= Enter;
        weapon.TriggerExit -= Exit;
        weapon.IDisable -= Close;
        anim.IOpenDamageCollider -= Open;
        anim.ICloseDamageCollider -= Close;
    }

    private void Enter(LayerMask mask, Collider ob)
    {
        Health health;

        if (ob.TryGetComponent<Health>(out health))
        {
            if (EnterObject.Contains(ob)) return;

            EnterObject.Add(ob);
            health.InflictDamage(20f);
        }
        else
        {

        }
    }

    private void Exit(LayerMask mask, Collider ob)
    {
        if (ob.TryGetComponent<Health>(out _))
        {
            if (EnterObject.Contains(ob))
                EnterObject.Remove(ob);
        }
    }

    private void Disable()
    {
        EnterObject.Clear();
    }

    private void Open()
    {
        weapon.collider.enabled = true;
    }

    private void Close()
    {
        weapon.collider.enabled = false;
    }
}
