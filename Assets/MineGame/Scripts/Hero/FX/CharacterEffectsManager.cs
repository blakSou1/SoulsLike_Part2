using System;

[Serializable]
public class CharacterEffectsManager
{
    public WeaponEffects weaponEffects;
    public AtackEffects atackEffects;

    public virtual void PlayWeaponFX(ItemActionContainerModel actionContainer = null)
    {
        weaponEffects.PlayWeaponFX();
        atackEffects.PlayAtackSlashFX(actionContainer);
    }
}
