using System;
using UnityEngine;

[Serializable]
public class KatanaIsNojn
{
    private ScabbardHolder scabbardHolder;
    private HolderInHand holderInHand;
    [SerializeField] private string animNameKatanaIsHolder;
    [SerializeField] private string animNameKatanaIsScabbard;

    public bool isHook;

    public void Init(GameObject thisObject)
    {
        scabbardHolder = thisObject.GetComponentInChildren<ScabbardHolder>();
        holderInHand = thisObject.GetComponentInChildren<HolderInHand>();

        PlayerView.animHook.katanaIsHook += KatanaIsHook;
    }
    public void OnDestroy()
    {
        PlayerView.animHook.katanaIsHook -= KatanaIsHook;
    }

    public void IsKatana()
    {
        isHook = !isHook;

        if (isHook)
            PlayerView.animHook.PlayTargetAnimation(animNameKatanaIsHolder, false);
        else
            PlayerView.animHook.PlayTargetAnimation(animNameKatanaIsScabbard, false);
    }

    private void KatanaIsHook()
    {
        Transform katana;
        if (isHook)
            katana = scabbardHolder.IsKatana();
        else
            katana = holderInHand.IsKatana();

        katana.SetParent(isHook ? holderInHand.transform : scabbardHolder.transform);
        
        
        katana.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        katana.localScale = Vector3.one;

        PlayerView.animHook.anim.SetBool("isKatana", !isHook);
    }
}
