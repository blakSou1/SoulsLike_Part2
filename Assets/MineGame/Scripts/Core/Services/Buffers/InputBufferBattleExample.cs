using UnityEngine;

public class InputBufferBattleExample : MonoBehaviour, IService
{
    [Header("InputBuffer")]
    [SerializeField] TimedInputActionInputBuffer AttackBuffer = new();

    [Header("InputBuffer")]
    [SerializeField] TimedInputActionInputBuffer ParryBuffer = new();
    [SerializeField] TimedInputActionInputBuffer ParryBufferCanceled = new();

    [Header("InputBuffer")]
    [SerializeField] TimedInputActionInputBuffer CtrlBuffer = new();

    public void Init()
    {
        ComboInputSubscription();
    }

    private void Update()
    {
        TryConsumeBufferedBattle(ParryBuffer);
        TryConsumeBufferedBattle(ParryBufferCanceled);

        TryConsumeBufferedBattle(AttackBuffer);

        TryConsumeBufferedBattle(CtrlBuffer);
    }

    private void OnDestroy()
    {
        ComboInputUnsubscription();
    }

    private void ComboInputSubscription()
    {
        G.inputs.Player.Attack.started += i => IsPressedThisFrame(new(i), AttackBuffer);
        G.inputs.Player.Parry.started += i => IsPressedThisFrame(new(i), ParryBuffer);
        G.inputs.Player.Ctrl.started += i => IsPressedThisFrame(new(i), CtrlBuffer);

        //G.inputs.Player.Attack.performed += i => IsPressedThisFrame(new(i), AttackBuffer);
        //G.inputs.Player.Parry.performed += i => IsPressedThisFrame(new(i), ParryBuffer);
        G.inputs.Player.Ctrl.performed += i => IsPressedThisFrame(new(i), CtrlBuffer);

        //G.inputs.Player.Attack.canceled += i => IsPressedThisFrame(new(i), AttackBuffer);
        G.inputs.Player.Parry.canceled += i => IsPressedThisFrame(new(i), ParryBufferCanceled);
        G.inputs.Player.Ctrl.canceled += i => IsPressedThisFrame(new(i), CtrlBuffer);
    }

    private void ComboInputUnsubscription()
    {
        G.inputs.Player.Attack.started -= i => IsPressedThisFrame(new(i), AttackBuffer);
        G.inputs.Player.Parry.started -= i => IsPressedThisFrame(new(i), ParryBuffer);
        G.inputs.Player.Ctrl.started -= i => IsPressedThisFrame(new(i), CtrlBuffer);

        //G.inputs.Player.Attack.performed -= i => IsPressedThisFrame(new(i), AttackBuffer);
        //G.inputs.Player.Parry.performed -= i => IsPressedThisFrame(new(i), ParryBuffer);
        G.inputs.Player.Ctrl.performed -= i => IsPressedThisFrame(new(i), CtrlBuffer);

        //G.inputs.Player.Attack.canceled -= i => IsPressedThisFrame(new(i), AttackBuffer);
        G.inputs.Player.Parry.canceled -= i => IsPressedThisFrame(new(i), ParryBuffer);
        G.inputs.Player.Ctrl.canceled -= i => IsPressedThisFrame(new(i), CtrlBuffer);
    }

    void TryConsumeBufferedBattle(TimedInputActionInputBuffer buffer)
    {
        if (!buffer.HasBuffer)
            return;

        if (!G.playerView.AnimHook.isInteracting)
        {
            if (G.playerView.finisherSystem.StartFinisher())
            {
                buffer.Consume();
                return;
            }

            G.playerView.ComboController.TargetSetMoveAction(buffer.Value);
            buffer.Consume();
            return;
        }

        if (G.playerView.AnimHook.canDoCombo)
            if (G.playerView.ComboController.DoCombo(buffer.Value))
            {
                buffer.Consume();
                return;
            }

        if (G.playerView.AnimHook.isInterrupt)
        {
            G.playerView.ComboController.TargetSetMoveAction(buffer.Value);
            buffer.Consume();
        }
    }

    void IsPressedThisFrame(BufferedInputData value, TimedInputActionInputBuffer buffer) {
        buffer.Set(value);
        TryConsumeBufferedBattle(buffer);
    }
}
