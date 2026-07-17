using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    public float damage = 0;

    public void InflictDamage(float addDamage)
    {
        damage += addDamage;

        if(maxHealth - damage < 0)
        {
            //TODO
        }
    }
}
