using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class ComboController
{
    public SetMoveProfile setMoveProfile;

    private ComboModel[] combo;
    [NonSerialized] public ItemActionContainerModel atackParam;

    public void LoadCombo(ComboModel[] targetCombo) =>
        combo = targetCombo;
    public void LoadAtackParam(ItemActionContainerModel atackParams) =>
    atackParam = atackParams;

    public bool DoCombo(BufferedInputData context, float time = 0)
    {
        ComboModel comb = GetComboFromInp(context);

        if (comb == null)
            return false;

        G.playerView.AnimHook.PlayTargetAnimation(comb.animName, true);
        G.playerView.AnimHook.canDoCombo = false;

        G.playerView.characterEffectsManager.PlayWeaponFX();

        return true;
    }
    
    public ComboModel GetComboFromInp(BufferedInputData context)
    {
        if (combo == null)
            return null;

        return combo.FirstOrDefault(c =>
        c.inp.action.name == context.actionName &&
        c.inputsPhase == context.phase
        );
    }

    public bool TargetSetMoveAction(BufferedInputData context, float time = 0)
    {
        if (setMoveProfile == null) return false;

        var list = setMoveProfile.atackInputs.FirstOrDefault(i =>
            i.input != null && i.input.action.name == context.actionName);  

        if (list == null) return false;

        var stt = list.inputStatsAction.Where(s => s.inputsPhase == context.phase).ToList();

        if (stt == null)
            return false;

        StateAction st = null;
        foreach (var i in stt)
        {
            if (i.GreatLess)
            {
                if (i.TimePressing < Time.time - time)
                    st = i;
            }
            else
            {
                if (i.TimePressing > Time.time - time)
                    st = i;
            }
        }

        if (st == null || st.inputStatsAction?.inputStatsAction == atackParam) return false;

        var ac = st.inputStatsAction.inputStatsAction;
        if (ac == null) return false;

        G.playerView.characterEffectsManager.PlayWeaponFX();
        G.playerView.AnimHook.PlayTargetAnimation(ac.animName, ac.isInteracting);
        return true;                                     
    }
}
