using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerView : MonoBehaviour
{
    public Rigidbody rb{ get; private set; }
    public AnimatorHookView AnimHook { get; private set; }

    [NonSerialized] public LockOnComponent LockOnComponent;
    [NonSerialized] public MotionWarpingSystem motionWarpingSystem;
    [SerializeField] private PlayerMovementComponent _playerMovement;
    [field: SerializeField] public ComboController ComboController{ get; private set; }

    private void Start()
    {
        G.playerView = this;

        AnimHook = GetComponentInChildren<AnimatorHookView>();
        rb = GetComponent<Rigidbody>();
        LockOnComponent = GetComponent<LockOnComponent>();
        motionWarpingSystem = GetComponentInChildren<MotionWarpingSystem>();

        _playerMovement.Init();
        ComboController.Init();
    }

    private void FixedUpdate()
    {
        if (!G.inputs.Player.Parry.IsPressed())
            AnimHook.Anim.SetBool("IsBlock", false);
        else
            AnimHook.Anim.SetBool("IsBlock", true);

        if (AnimHook.isInteracting)
            AnimHook.isInteracting = AnimHook.Anim.GetBool("isInteracting");
        else
            AnimHook.isInterrupt = true;
        _playerMovement.Tick();
    }

    private void OnDestroy()
    {
        _playerMovement.Dispose();
        LockOnComponent.Dispose();
        ComboController.Dispose();
    }
}