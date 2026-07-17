using System;

[Serializable]
public class CharacterEffectsManager
{
    public WeaponEffects weaponEffects;
    public AtackEffects atackEffects;

    public virtual void PlayWeaponFX()
    {
        weaponEffects.PlayWeaponFX();
        atackEffects.PlayAtackSlashFX();
    }
}
