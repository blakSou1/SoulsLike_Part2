using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(LockOnComponent))]
public class PlayerView : MonoBehaviour
{
    public Rigidbody rb{ get; private set; }
    public AnimatorHookView AnimHook { get; private set; }

    [NonSerialized] public LockOnComponent LockOnComponent;
    [NonSerialized] public MotionWarpingSystem motionWarpingSystem;
    [NonSerialized] public HealthController healthController;
    [NonSerialized] public FinisherSystem finisherSystem;
    public PlayerMovementComponent playerMovementComponent;
    [field: SerializeField] public CharacterEffectsManager characterEffectsManager { get; private set; }
    [field: SerializeField] public ComboController ComboController{ get; private set; }

    private void Awake()
    {
        G.playerView = this;

        AnimHook = GetComponentInChildren<AnimatorHookView>();
        rb = GetComponent<Rigidbody>();
        LockOnComponent = GetComponent<LockOnComponent>();
        motionWarpingSystem = GetComponentInChildren<MotionWarpingSystem>();
        healthController = GetComponent<HealthController>();
        healthController.weapon = GetComponentInChildren<Weapon>();
        finisherSystem = GetComponent<FinisherSystem>();

        playerMovementComponent.Init();
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
        playerMovementComponent.Tick();
    }

    private void OnDestroy()
    {
        playerMovementComponent.Dispose();
    }
}