using UnityEngine;
using Zenject;

public class PlayerView : MonoBehaviour
{
    #region Component
    public PlayerInput input{ get; private set; }

    public Rigidbody rb{ get; private set; }
    public AnimatorHookView animHook{ get; private set; }
    [field: SerializeField]public ComboController comboController{ get; private set; }
    [SerializeField] private PlayerMovementComponent _playerMovement;
    [field: SerializeField]public LockOnComponent lockOnComponent{ get; private set; }
    #endregion

    [Inject]
    private void Construct(PlayerInput input)
    {
        this.input = input;
    }

    private void Start()
    {
        animHook = GetComponentInChildren<AnimatorHookView>();
        rb = GetComponent<Rigidbody>();

        _playerMovement.Init(this);
        lockOnComponent.Init(this);
        comboController.Init(this);
    }
    private void OnDestroy()
    {
        _playerMovement.Dispose();
        lockOnComponent.Dispose();
        comboController.Dispose();
    }
    private void FixedUpdate()
    {
        if (animHook.isInteracting)
            animHook.isInteracting = animHook.anim.GetBool("isInteracting");

        _playerMovement.Update();
    }

}