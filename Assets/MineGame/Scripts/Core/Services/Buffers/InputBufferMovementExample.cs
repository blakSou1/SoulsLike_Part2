using UnityEngine;

public class InputBufferMovementExample : MonoBehaviour, IService
{
    public void Init()
    {
        PlayerInputSubscription();
    }

    public void FixedUpdate()
    {
        if (!G.playerView.AnimHook.Anim.GetBool("IsBlock"))
            return;

        G.playerView.playerMovementComponent.isSprint = false;
    }

    private void OnDestroy()
    {
        PlayerInputUnsubscription();
    }

    private void PlayerInputSubscription()
    {
        G.inputs.Player.Move.performed += i => G.playerView.playerMovementComponent.inputMoveDirection = i.ReadValue<Vector2>();
        G.inputs.Player.Move.canceled += i => G.playerView.playerMovementComponent.inputMoveDirection = Vector2.zero;

        G.inputs.Player.Sprint.started += i => G.playerView.playerMovementComponent.isSprint = true;
        G.inputs.Player.Sprint.canceled += i => G.playerView.playerMovementComponent.isSprint = false;
    }

    private void PlayerInputUnsubscription()
    {
        G.inputs.Player.Move.performed -= i => G.playerView.playerMovementComponent.inputMoveDirection = i.ReadValue<Vector2>();
        G.inputs.Player.Move.canceled -= i => G.playerView.playerMovementComponent.inputMoveDirection = Vector2.zero;

        G.inputs.Player.Sprint.started -= i => G.playerView.playerMovementComponent.isSprint = true;
        G.inputs.Player.Sprint.canceled -= i => G.playerView.playerMovementComponent.isSprint = false;
    }
}
