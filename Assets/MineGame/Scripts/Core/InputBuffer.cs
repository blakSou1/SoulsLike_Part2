using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputBuffer : MonoBehaviour, IService
{
    [Header("Param")]
    [ReadOnly] public bool isSprint;
    [ReadOnly] public Vector3 inputMoveDirection;

    [ReadOnly] public Action LockOn;
    [ReadOnly] public Action<int> SwitchTarget;
    [ReadOnly] public Action<BufferedInputData> HandleBattle;

    [Header("Buffer")]
    [SerializeField] private float timers = .55f;
    private float time = 0;
    private BufferedInputData BufferHandle;
    public void Init()
    {
        PlayerInputSubscription();
        LockOnInputSubscription();
        ComboInputSubscription();
    }

    private void OnDestroy()
    {
        PlayerInputUnsubscription();
        LockOnInputUnsubscription();
        ComboInputUnsubscription();
    }

    //
    private void ComboInputSubscription()
    {
        G.inputs.Player.Attack.started += i => IsHandleBatle(i);
        G.inputs.Player.Parry.started += i => IsHandleBatle(i);
        G.inputs.Player.Ctrl.started += i => IsHandleBatle(i);

        G.inputs.Player.Attack.performed += i => IsHandleBatle(i);
        G.inputs.Player.Parry.performed += i => IsHandleBatle(i);
        G.inputs.Player.Ctrl.performed += i => IsHandleBatle(i);

        G.inputs.Player.Attack.canceled += i => IsHandleBatle(i);
        G.inputs.Player.Parry.canceled += i => IsHandleBatle(i);
        G.inputs.Player.Ctrl.canceled += i => IsHandleBatle(i);
    }

    private void ComboInputUnsubscription()
    {
        G.inputs.Player.Attack.started -= i => IsHandleBatle(i);
        G.inputs.Player.Parry.started -= i => IsHandleBatle(i);
        G.inputs.Player.Ctrl.started -= i => IsHandleBatle(i);

        G.inputs.Player.Attack.performed -= i => IsHandleBatle(i);
        G.inputs.Player.Parry.performed -= i => IsHandleBatle(i);
        G.inputs.Player.Ctrl.performed -= i => IsHandleBatle(i);

        G.inputs.Player.Attack.canceled -= i => IsHandleBatle(i);
        G.inputs.Player.Parry.canceled -= i => IsHandleBatle(i);
        G.inputs.Player.Ctrl.canceled -= i => IsHandleBatle(i);
    }
    //
    private void LockOnInputSubscription()
    {
        G.inputs.Player.LockOn.started += i => LockOn?.Invoke();
        G.inputs.Player.NewTargetLock.started += i =>
        {
            float inputValue = i.ReadValue<float>();
            int deadZone = 70;

            if (Mathf.Abs(inputValue) > deadZone)
                SwitchTarget?.Invoke(inputValue > 0 ? 1 : -1);
        };
    }

    private void LockOnInputUnsubscription()
    {
        G.inputs.Player.LockOn.started -= i => LockOn?.Invoke();
        G.inputs.Player.NewTargetLock.started -= i =>
        {
            float inputValue = i.ReadValue<float>();
            int deadZone = 20;

            if (Mathf.Abs(inputValue) > deadZone)
                SwitchTarget?.Invoke(inputValue > 0 ? 1 : -1);
        };
    }
    //
    private void PlayerInputSubscription()
    {
        G.inputs.Player.Move.performed += i => inputMoveDirection = i.ReadValue<Vector2>();
        G.inputs.Player.Move.canceled += i => inputMoveDirection = Vector2.zero;

        G.inputs.Player.Sprint.started += i => isSprint = true;
        G.inputs.Player.Sprint.canceled += i => isSprint = false;
    }

    private void PlayerInputUnsubscription()
    {
        G.inputs.Player.Move.performed -= i => inputMoveDirection = i.ReadValue<Vector2>();
        G.inputs.Player.Move.canceled -= i => inputMoveDirection = Vector2.zero;

        G.inputs.Player.Sprint.started -= i => isSprint = true;
        G.inputs.Player.Sprint.canceled -= i => isSprint = false;
    }

    Coroutine cor;
    private void IsHandleBatle(InputAction.CallbackContext i)
    {

        if (G.playerView.AnimHook.canDoCombo)
        {
            HandleBattle?.Invoke(new(i));
            return;
        }

        if (G.playerView.AnimHook.isInterrupt)
            HandleBattle?.Invoke(new(i));
        else
        {
            ComboModel comb = G.playerView.ComboController.GetComboFromInp(new(i));

            if (comb == null)
            {
                InputList matchingInputList = G.playerView.ComboController.setMoveProfile.atackInputs
                .FirstOrDefault(input => input.input.action.name == i.action.name);

                if (matchingInputList == null) return;

                StateAction matchingStateAction = matchingInputList.inputStatsAction
                    .FirstOrDefault(state => state.inputsPhase == i.phase);

                if (matchingStateAction == null) return;
            }

            time = Time.time;
            BufferHandle = new(i);

            cor ??= StartCoroutine(Timer());
        }
    }
    
    private IEnumerator Timer()
    {
        while (Time.time - time < timers)
        {
            if (G.playerView.AnimHook.isInterrupt || G.playerView.AnimHook.canDoCombo)
                HandleBattle?.Invoke(BufferHandle);

            yield return new WaitForFixedUpdate();
        }

        cor = null;
    }

    public void ClearBuffer()
    {
        StopAllCoroutines();
        cor = null;
    }
}

public class BufferedInputData
{
    public string actionName;
    public InputActionPhase phase;

    public BufferedInputData(InputAction.CallbackContext context)
    {
        actionName = context.action.name;
        phase = context.phase;
    }
}
