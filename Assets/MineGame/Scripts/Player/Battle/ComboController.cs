using System;
using System.Linq;

[Serializable]
public class ComboController
{
    public SetMoveProfile setMoveProfile;

    private ComboModel[] combo;

    public void LoadCombo(ComboModel[] targetCombo) =>
        combo = targetCombo;

    #region Life
    public void Init()
    {
        G.inputBuffer.HandleBattle += HandleAtacking;
    }

    public void Dispose()
    {
        G.inputBuffer.HandleBattle -= HandleAtacking;
    }
    #endregion

    public bool DoCombo(BufferedInputData context)
    {
        ComboModel comb = GetComboFromInp(context);

        if (comb == null)
            return false;

        G.playerView.AnimHook.PlayTargetAnimation(comb.animName, true);
        G.playerView.AnimHook.canDoCombo = false;

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

    private void HandleAtacking(BufferedInputData context)
    {
        if (G.playerView.AnimHook.canDoCombo)
            if (DoCombo(context))
            {
                G.inputBuffer.ClearBuffer();
                return;
            }

        if (!G.playerView.AnimHook.isInteracting)
            TargetSetMoveAction(context);
        else if (G.playerView.AnimHook.isInterrupt)
        {
            G.inputBuffer.ClearBuffer();

            TargetSetMoveAction(context);
        }
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

        ItemActionContainerModel actionContainer = matchingStateAction.inputStatsAction;

        if (actionContainer == null) return;

        G.playerView.AnimHook.PlayTargetAnimation(actionContainer.animName, actionContainer.isInteracting);
    }
}
