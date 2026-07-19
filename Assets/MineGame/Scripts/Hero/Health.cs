using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    public float damage = 0;

    public virtual void InflictDamage(float addDamage)
    {
        if (maxHealth - damage < 0)
            return;

        damage += addDamage;

        if(maxHealth - damage < 0)
        {
            //TODO
        }
    }
}
