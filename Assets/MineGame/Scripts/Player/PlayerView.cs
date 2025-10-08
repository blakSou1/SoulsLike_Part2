using UnityEngine;

public class PlayerView : MonoBehaviour
{
    #region Component
    public Rigidbody rb{ get; private set; }
    public AnimatorHookView animHook{ get; private set; }
    [field: SerializeField]public ComboController comboController{ get; private set; }
    [SerializeField] private PlayerMovementComponent _playerMovement;
    [field: SerializeField]public LockOnComponent lockOnComponent{ get; private set; }
    #endregion

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