using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerView : MonoBehaviour
{
    public Rigidbody Rb{ get; private set; }
    public AnimatorHookView AnimHook { get; private set; }
    
    [field: SerializeField]public ComboController ComboController{ get; private set; }
    [SerializeField] private PlayerMovementComponent _playerMovement;
    [field: SerializeField] public LockOnComponent LockOnComponent{ get; private set; }

    private void Awake()
    {
        if (G.input == null)
        {
            G.input = new();
            G.input.Player.Enable();
        }
    }
    private void Start()
    {
        AnimHook = GetComponentInChildren<AnimatorHookView>();
        Rb = GetComponent<Rigidbody>();

        _playerMovement.Init(this);
        LockOnComponent.Init(this);
        ComboController.Init(this);
    }
    private void FixedUpdate()
    {
        if (AnimHook.isInteracting)
            AnimHook.isInteracting = AnimHook.Anim.GetBool("isInteracting");

        _playerMovement.Update();
    }
    private void OnDestroy()
    {
        _playerMovement.Dispose();
        LockOnComponent.Dispose();
        ComboController.Dispose();
    }
}