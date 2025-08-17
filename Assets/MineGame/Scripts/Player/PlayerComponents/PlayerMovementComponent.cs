using System;
using UnityEngine;

[Serializable]
public class PlayerMovementComponent : IDisposable
{
    #region Movement
    [ReadOnly] public bool isSprint;
    [ReadOnly] public float moveAmount;
    private readonly float movementSpeed = 3f;
    private readonly float sprintSpeed = 9f;
    [ReadOnly] public float currentSpeed;
    private Vector2 currentPosition;
    [ReadOnly] public Vector3 inputMoveDirection;
    [ReadOnly] public Vector3 deltaPosition;
    #endregion

    #region rotation
    private readonly float rotationSpeed = 10f;
    private readonly float atackRotationSpeed = 10f;
    private Transform rotationMesh;
    #endregion

    #region GroundCheck
    private bool isOnAir;
    [ReadOnly] public bool isGrounded;
    private RaycastHit hit;
    [SerializeField] private LayerMask groundCheckMask = 1 << 6;
    #endregion

    private PlayerView _playerView;

    #region Life
    public void Init(PlayerView playerView)
    {
        _playerView = playerView;
        rotationMesh = _playerView.transform.GetChild(0);

        _playerView.animHook.DeltaPositionAnimator += DeltaPosition;

        _playerView.input.Player.Move.performed += i => inputMoveDirection = i.ReadValue<Vector2>();
        _playerView.input.Player.Move.canceled += i => inputMoveDirection = Vector2.zero;

        _playerView.input.Player.Sprint.started += i => isSprint = true;
        _playerView.input.Player.Sprint.canceled += i => isSprint = false;

    }

    public void Dispose()
    {
        _playerView.animHook.DeltaPositionAnimator -= DeltaPosition;

        _playerView.input.Player.Move.performed -= i => inputMoveDirection = i.ReadValue<Vector2>();
        _playerView.input.Player.Move.canceled -= i => inputMoveDirection = Vector2.zero;

        _playerView.input.Player.Sprint.started -= i => isSprint = true;
        _playerView.input.Player.Sprint.canceled -= i => isSprint = false;
    }

    public void Update()
    {
        CheckGround();
        Movement();
    }
    #endregion

    private void Movement()
    {
        float targetMoveAmount = Mathf.Clamp01(Mathf.Abs(inputMoveDirection.y) + Mathf.Abs(inputMoveDirection.x));
        moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.deltaTime / .15f);

        Vector3 movementDirection = Camera.main.transform.right * inputMoveDirection.x;
        movementDirection += Camera.main.transform.forward * inputMoveDirection.y;
        movementDirection.Normalize();

        Vector3 targetVelocity;

        //HANDLE ROTATION
        if (!_playerView.animHook.isInteracting || _playerView.animHook.canRotate)
        {
            Vector3 rotationDir = movementDirection;

            if (_playerView.lockOnComponent.lockOn)
            {
                if (!isSprint)
                {
                    HandleRotation(_playerView.transform.forward, true);
                    rotationDir = _playerView.lockOnComponent.currentLockable.GetLockOnTarget(_playerView.transform).position - _playerView.transform.position;
                }
                else
                {
                    HandleRotation(rotationDir, true);
                    rotationDir = _playerView.lockOnComponent.currentLockable.GetLockOnTarget(_playerView.transform).position - _playerView.transform.position;
                }
            }
            else
                HandleRotation(_playerView.transform.forward, true);
                
            HandleRotation(rotationDir);
        }

        //HANDLE SPEED
        if (_playerView.lockOnComponent.lockOn && !isSprint)
        {
            targetVelocity = movementSpeed * inputMoveDirection.y * _playerView.transform.forward;
            targetVelocity += movementSpeed * inputMoveDirection.x * _playerView.transform.right;
        }
        else
        {
            currentSpeed = movementSpeed;
            if (isSprint)
                currentSpeed = sprintSpeed;

            targetVelocity = movementDirection * currentSpeed;
        }

        //HANDLE ANIMATION MOVEMENT
        if (_playerView.animHook.isInteracting && !_playerView.animHook.canMove)
            targetVelocity = deltaPosition * 1;

        //HANDLE MOVEMENT
        if (isGrounded)
        {
            Vector3 currentNormal = hit.normal;
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, currentNormal);

            _playerView.rb.linearVelocity = targetVelocity;

            Vector3 grondedPosition = _playerView.transform.position;
            grondedPosition.y = currentPosition.y;
            _playerView.transform.position = Vector3.Lerp(_playerView.transform.position, grondedPosition, Time.deltaTime / .1f);

            HandleAnimations();
        }
    }
    private void DeltaPosition(Vector3 deltaPosition)
    {
        if (_playerView.animHook.isInteracting == false)
            return;

        if (isGrounded && Time.deltaTime > 0)
            this.deltaPosition = deltaPosition;
    }

    private void HandleRotation(Vector3 targetDir, bool sprintIsLock = false)
    {
        Transform targetTransform = sprintIsLock ? rotationMesh : _playerView.transform;

        float moveOverride = moveAmount;
        if (_playerView.lockOnComponent.lockOn)
            moveOverride = 1;

        targetDir.Normalize();
        targetDir.y = 0;
        if (targetDir == Vector3.zero)
            targetDir = targetTransform.forward;

        float actualRotationSpeed = rotationSpeed;
        if (_playerView.animHook.isInteracting)
            actualRotationSpeed = atackRotationSpeed;

        Quaternion tr = Quaternion.LookRotation(targetDir);
        Quaternion targetRotation = Quaternion.Slerp(
            targetTransform.rotation,
            tr,
            Time.deltaTime * moveOverride * actualRotationSpeed);

        targetTransform.rotation = targetRotation;
    }

    private void CheckGround()
    {
        Vector3 origin = _playerView.transform.position;
        origin.y += .5f;

        float dis = .4f;
        if (isOnAir)
            dis = .5f;

        Debug.DrawRay(origin, Vector3.down * dis, Color.red);
        if (Physics.SphereCast(origin, .2f, Vector3.down, out hit, dis, groundCheckMask))
        {
            isGrounded = true;
            currentPosition = hit.point;
            if (hit.point.y - _playerView.transform.position.y < .5f)
                currentPosition.y = hit.point.y;
            Vector3 currentNormal = hit.normal;

            float angle = Vector3.Angle(Vector3.up, currentNormal);
            if (angle > 45)
                isGrounded = false;
            if (isOnAir)
                isOnAir = false;
        }
        else
        {
            if (isGrounded)
                isGrounded = false;
            if (isOnAir == false)
                isOnAir = true;
        }
    }

    private void HandleAnimations()
    {
        _playerView.animHook.anim.SetBool("isSprint", isSprint);
        _playerView.animHook.anim.SetFloat("moveAmount", moveAmount);

        float f = currentSpeed;
        if (moveAmount < .4f)
            f = 0;

        if (_playerView.lockOnComponent.lockOn && !isSprint)
        {
            float ver = 0;
            float hor = 0;

            if (f > 0)
            {
                if (inputMoveDirection.y != 0)
                    if (inputMoveDirection.y > 0)
                        ver = currentSpeed;
                    else
                        ver = -currentSpeed;

                if (inputMoveDirection.x != 0)
                    if (inputMoveDirection.x > 0)
                        hor = currentSpeed;
                    else
                        hor = -currentSpeed;
            }

            _playerView.animHook.anim.SetFloat("forward", ver, .3f, Time.deltaTime);
            _playerView.animHook.anim.SetFloat("sideways", hor, .3f, Time.deltaTime);
        }
        else
        {
            _playerView.animHook.anim.SetFloat("forward", f, .1f, Time.deltaTime);
            _playerView.animHook.anim.SetFloat("sideways", 0);
        }
    }

}