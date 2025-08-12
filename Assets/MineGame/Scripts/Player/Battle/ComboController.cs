using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class ComboController
{
    [SerializeField] private SetMoveProfile setMoveProfile;

    private ComboModel[] combo;

    private PlayerView _playerView;

    public void LoadCombo(ComboModel[] targetCombo) =>
        combo = targetCombo;

    #region Life
    public void Init(PlayerView playerView)
    {
        _playerView = playerView;

        _playerView.input.Player.Attack.started += i => HandleAtacking(i);
        _playerView.input.Player.Parry.started += i => HandleAtacking(i);
        _playerView.input.Player.Ctrl.started += i => HandleAtacking(i);

        _playerView.input.Player.Attack.performed += i => HandleAtacking(i);
        _playerView.input.Player.Parry.performed += i => HandleAtacking(i);
        _playerView.input.Player.Ctrl.performed += i => HandleAtacking(i);

        _playerView.input.Player.Attack.canceled += i => HandleAtacking(i);
        _playerView.input.Player.Parry.canceled += i => HandleAtacking(i);
        _playerView.input.Player.Ctrl.canceled += i => HandleAtacking(i);
    }

    public void Dispose()
    {
        _playerView.input.Player.Attack.started -= i => HandleAtacking(i);
        _playerView.input.Player.Parry.started -= i => HandleAtacking(i);
        _playerView.input.Player.Ctrl.started -= i => HandleAtacking(i);

        _playerView.input.Player.Attack.performed -= i => HandleAtacking(i);
        _playerView.input.Player.Parry.performed -= i => HandleAtacking(i);
        _playerView.input.Player.Ctrl.performed -= i => HandleAtacking(i);

        _playerView.input.Player.Attack.canceled -= i => HandleAtacking(i);
        _playerView.input.Player.Parry.canceled -= i => HandleAtacking(i);
        _playerView.input.Player.Ctrl.canceled -= i => HandleAtacking(i);
    }
    #endregion

    public void DoCombo(InputAction.CallbackContext context)
    {
        ComboModel comb = GetComboFromInp(context);

        if (comb == null)
            return;

        _playerView.animHook.PlayTargetAnimation(comb.animName, true);
        _playerView.animHook.canDoCombo = false;
    }
    private ComboModel GetComboFromInp(InputAction.CallbackContext context)
    {
        if (combo == null)
            return null;

        for (int i = 0; i < combo.Length; i++)
        {
            if (combo[i].inp.action == context.action)
                return combo[i];
        }

        return null;
    }

    private void HandleAtacking(InputAction.CallbackContext context)
    {
        if (!_playerView.animHook.isInteracting)
            TargetSetMoveAction(context);
        else
        {
            if (_playerView.animHook.canDoCombo)
                DoCombo(context);
        }
    }
    public void TargetSetMoveAction(InputAction.CallbackContext context)
    {
        if (setMoveProfile == null) return;

        InputList matchingInputList = setMoveProfile.atackInputs
            .FirstOrDefault(input => input.atackInputs.action == context.action);

        if (matchingInputList == null) return;

        StateAction matchingStateAction = matchingInputList.inputStatsAction
            .FirstOrDefault(state => state.inputsPhase == context.phase);

        if (matchingStateAction == null) return;

        ItemActionContainerModel actionContainer = matchingStateAction.inputStatsAction;

        if (actionContainer == null) return;

        _playerView.animHook.PlayTargetAnimation(actionContainer.animName, actionContainer.isInteracting);
    }
}