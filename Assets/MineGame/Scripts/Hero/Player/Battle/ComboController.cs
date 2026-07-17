using System;
using System.Linq;

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

    public bool DoCombo(BufferedInputData context)
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
    
    public void TargetSetMoveAction(BufferedInputData context)
    {
        if (setMoveProfile == null) return;

        InputList matchingInputList = setMoveProfile.atackInputs
            .FirstOrDefault(input => input.input.action.name == context.actionName);
            
        if (matchingInputList == null) return;

        StateAction matchingStateAction = matchingInputList.inputStatsAction
            .FirstOrDefault(state => state.inputsPhase == context.phase);

        if (matchingStateAction == null) return;

        ItemActionContainerModel actionContainer = matchingStateAction.inputStatsAction.inputStatsAction;

        if (actionContainer == null) return;

        G.playerView.characterEffectsManager.PlayWeaponFX();
        G.playerView.AnimHook.PlayTargetAnimation(actionContainer.animName, actionContainer.isInteracting);
    }
}
