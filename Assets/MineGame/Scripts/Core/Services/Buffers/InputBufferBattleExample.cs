using UnityEngine;
using UnityEngine.InputSystem;

public class InputBufferBattleExample : MonoBehaviour, IService
{
    [Header("InputBuffer")]
    [SerializeField] TimedInputActionInputBuffer AttackBuffer = new();
    [SerializeField] TimedInputActionInputBuffer OffAttackBuffer = new();

    [Header("InputBuffer")]
    [SerializeField] TimedInputActionInputBuffer ParryBuffer = new();
    [SerializeField] TimedInputActionInputBuffer ParryBufferCanceled = new();

    [Header("InputBuffer")]
    [SerializeField] TimedInputActionInputBuffer CtrlBuffer = new();

    private float timer = 0f;

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
        G.inputs.Player.Attack.started += OnAttack;
        G.inputs.Player.Attack.canceled += OfAttack;
        G.inputs.Player.Parry.started += OnParry;
        G.inputs.Player.Parry.canceled += OnParryCanceled;
    }

    private void OnAttack(InputAction.CallbackContext c)
    {
        G.playerView.AnimHook.wasReleased = false;
        timer = Time.time;
        Push(AttackBuffer, c);
    }
    private void OfAttack(InputAction.CallbackContext c)
    {
        G.playerView.AnimHook.wasReleased = true;
        Push(OffAttackBuffer, c);
        timer = 0f;
    }

    private void OnParry(InputAction.CallbackContext c) => Push(ParryBuffer, c);
    private void OnParryCanceled(InputAction.CallbackContext c) => Push(ParryBufferCanceled, c);

    private void Push(TimedInputActionInputBuffer b, InputAction.CallbackContext c)
    {
        if (G.playerView == null) return;
        b.Set(new BufferedInputData(c));
        TryConsumeBufferedBattle(b);
    }

    private void ComboInputUnsubscription()
    {
        G.inputs.Player.Attack.started -= OnAttack;
        G.inputs.Player.Attack.canceled -= OfAttack;
        G.inputs.Player.Parry.started -= OnParry;
        G.inputs.Player.Parry.canceled -= OnParryCanceled;
    }

    void TryConsumeBufferedBattle(TimedInputActionInputBuffer buffer)
    {
        if (!buffer.HasBuffer || G.playerView == null) return;
        var hook = G.playerView.AnimHook;

        // 1) финишер — только для атаки
        if (buffer == AttackBuffer && (hook.canDoCombo || hook.isInterrupt || !hook.isInteracting))
            if (G.playerView.finisherSystem != null && G.playerView.finisherSystem.StartFinisher())
            { buffer.Consume(); return; }

        // 2) КОМБО — раньше, чем «не в взаимодействии»   ← ключевая перестановка
        if (hook.canDoCombo && G.playerView.ComboController.DoCombo(buffer.Value, timer))
        { buffer.Consume(); return; }

        // 3) первый удар / смена движения — только если реально сработало
        if ((!hook.isInteracting || hook.isInterrupt)
            && G.playerView.ComboController.TargetSetMoveAction(buffer.Value, timer))
        { buffer.Consume(); return; }

        // 4) не влезло → НЕ consume: ввод ждёт до истечения holdTime (120 мс)
    }

}
