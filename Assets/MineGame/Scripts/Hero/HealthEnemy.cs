
public class HealthEnemy : Health
{
    public Enemy enemy;

    private void Awake()
    {
        if(enemy == null)
            enemy = GetComponentInChildren<Enemy>();    
    }

    public override void InflictDamage(float addDamage)
    {
        if (maxHealth - damage < 0)
            return;

        damage += addDamage;

        if (maxHealth - damage < 0)
            G.playerView.finisherSystem.Knockout(enemy);//TODO
    }

}
