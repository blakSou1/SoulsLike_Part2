using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

public class PlayerView : MonoBehaviour
{
    #region Param
    #region Battle
    [SerializeField] private SetMoveProfile setMoveProfile;
    #endregion

    #region Move
    private bool isSprint;
    private float moveAmount;
    private readonly float movementSpeed = 3f;
    private readonly float sprintSpeed = 9f;
    private float currentSpeed;
    private Vector2 currentPosition;
    private Vector3 inputMoveDirection;
    private Vector3 deltaPosition;

    #region GroundCheck
    private bool isOnAir;
    private bool isGrounded;
    private RaycastHit hit;
    private LayerMask groundCheckMask = 1 << 6;
    #endregion

    #region rotation
    private readonly float rotationSpeed = 10f;
    private readonly float atackRotationSpeed = 10f;
    #endregion
    #endregion

    #region LockOn
    [SerializeField] private float maxLockDistance = 15f;
    [SerializeField] private float fieldOfView = 60f;
    private bool lockOn = false;

    private List<ILockable> potentialTargets = new();
    private ILockable currentLockable;

    private LayerMask LockOnMask = 1 << 3;
    [SerializeField] private CinemachineCamera FollowCinemachine;
    #endregion

    #region Component
    [Inject] private PlayerInput input;

    private Transform camTransform;
    private Rigidbody rb;
    public static AnimatorHookView animHook;
    public static ComboController comboController;
    #endregion
    #endregion

    private void Start()
    {
        #region Init
        comboController = new();
        animHook = GetComponentInChildren<AnimatorHookView>();
        camTransform = Camera.main.transform;
        rb = GetComponent<Rigidbody>();
        #endregion

        #region Event
        animHook.DeltaPositionAnimator += DeltaPosition;
        #endregion

        #region Input
        #region Move
        input.Player.Move.performed += i => inputMoveDirection = i.ReadValue<Vector2>();
        input.Player.Move.canceled += i => inputMoveDirection = Vector2.zero;

        input.Player.Sprint.started += i => isSprint = true;
        input.Player.Sprint.canceled += i => isSprint = false;
        #endregion
        #region LockOn
        input.Player.LockOn.started += i => LockOn();
        input.Player.NewTargetLock.started += i =>
        {
            float inputValue = i.ReadValue<float>();
            int deadZone = 20;

            if (Mathf.Abs(inputValue) > deadZone)
                SwitchTarget(inputValue > 0 ? 1 : -1);
        };
        #endregion
        #region Action
        input.Player.Attack.started += i => HandleAtacking(AtackInputs.lkm, InputStats.started);
        input.Player.Parry.started += i => HandleAtacking(AtackInputs.pkm, InputStats.started);
        input.Player.Ctrl.started += i => HandleAtacking(AtackInputs.Ctrl, InputStats.started);

        input.Player.Attack.performed += i => HandleAtacking(AtackInputs.lkm, InputStats.performed);
        input.Player.Parry.performed += i => HandleAtacking(AtackInputs.pkm, InputStats.performed);
        input.Player.Ctrl.performed += i => HandleAtacking(AtackInputs.Ctrl, InputStats.performed);

        input.Player.Attack.canceled += i => HandleAtacking(AtackInputs.lkm, InputStats.canceled);
        input.Player.Parry.canceled += i => HandleAtacking(AtackInputs.pkm, InputStats.canceled);
        input.Player.Ctrl.canceled += i => HandleAtacking(AtackInputs.Ctrl, InputStats.canceled);
        #endregion
        #endregion
    }
    private void OnDestroy()
    {
        #region Event
        animHook.DeltaPositionAnimator -= DeltaPosition;
        #endregion

        #region Input
        #region Move
        input.Player.Move.performed -= i => inputMoveDirection = i.ReadValue<Vector2>();
        input.Player.Move.canceled -= i => inputMoveDirection = Vector2.zero;

        input.Player.Sprint.started -= i => isSprint = true;
        input.Player.Sprint.canceled -= i => isSprint = false;
        #endregion
        #region LockOn
        input.Player.LockOn.started -= i => LockOn();
        input.Player.NewTargetLock.started -= i =>
        {
            float inputValue = i.ReadValue<float>();
            int deadZone = 20;

            if (Mathf.Abs(inputValue) > deadZone)
                SwitchTarget(inputValue > 0 ? 1 : -1);
        };
        #endregion
        #region Action
        input.Player.Attack.started -= i => HandleAtacking(AtackInputs.lkm, InputStats.started);
        input.Player.Parry.started -= i => HandleAtacking(AtackInputs.pkm, InputStats.started);
        input.Player.Ctrl.started -= i => HandleAtacking(AtackInputs.Ctrl, InputStats.started);

        input.Player.Attack.performed -= i => HandleAtacking(AtackInputs.lkm, InputStats.performed);
        input.Player.Parry.performed -= i => HandleAtacking(AtackInputs.pkm, InputStats.performed);
        input.Player.Ctrl.performed -= i => HandleAtacking(AtackInputs.Ctrl, InputStats.performed);

        input.Player.Attack.canceled -= i => HandleAtacking(AtackInputs.lkm, InputStats.canceled);
        input.Player.Parry.canceled -= i => HandleAtacking(AtackInputs.pkm, InputStats.canceled);
        input.Player.Ctrl.canceled -= i => HandleAtacking(AtackInputs.Ctrl, InputStats.canceled);
        #endregion
        #endregion
    }
    private void FixedUpdate()
    {
        CheckGround();
        Movement();
    }

    #region Atacking
    private void HandleAtacking(AtackInputs atackInput, InputStats statsClick)
    {
        if (!animHook.isInteracting)
            TargetSetMoveAction(atackInput, statsClick);
        else
        {
            if (animHook.canDoCombo)
                comboController.DoCombo(atackInput);
        }
    }
    public void TargetSetMoveAction(AtackInputs atackInput, InputStats statsClick)
    {
        if (setMoveProfile == null) return;

        InputList matchingInputList = setMoveProfile.atackInputs
            .FirstOrDefault(input => input.atackInputs == atackInput);

        if (matchingInputList == null) return;

        StateAction matchingStateAction = matchingInputList.inputStatsAction
            .FirstOrDefault(state => state.inputsState == statsClick);

        if (matchingStateAction == null) return;

        ItemActionContainerModel actionContainer = matchingStateAction.inputStatsAction;

        if (actionContainer == null) return;

        animHook.PlayTargetAnimation(actionContainer.animName, actionContainer.isInteracting);
    }
    #endregion

    #region ILock
    private void LockOn()
    {
        if (lockOn)
            DisableLockOn();
        else
        {
            FindLockableTarget();
            currentLockable = SelecktBesttarget();

            if (currentLockable != null)
            {
                FollowCinemachine.LookAt = currentLockable.GetLockOnTarget(transform);
                lockOn = true;
            }
        }
        FollowCinemachine.gameObject.SetActive(lockOn);
    }
    #region sub
    private ILockable SelecktBesttarget()
    {
        ILockable bestTarget = null;
        float maxScore = -Mathf.Infinity;

        foreach (ILockable target in potentialTargets)
        {
            if (!IsVisible(target)) continue;

            float distanceScore = 1 - Mathf.Clamp01(
                Vector3.Distance(transform.position, target.GetLockOnTarget(transform).position) / maxLockDistance
            );

            float finalScore =
                GetAnglePriority(target) * 0.5f
                + distanceScore * 0.3f;

            if (finalScore > maxScore)
            {
                maxScore = finalScore;
                bestTarget = target;
            }
        }
        return bestTarget;
    }
    private float GetAnglePriority(ILockable enemy)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(enemy.GetLockOnTarget(transform).position);
        Vector3 screenCenter = new(Screen.width / 2, Screen.height / 2, 0);
        return 1 - (Vector3.Distance(screenPos, screenCenter) / Screen.width);
    }
    private bool IsVisible(ILockable target)
    {
        Vector3 dir = (target.GetLockOnTarget(transform).position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        return angle <= fieldOfView * 0.5f
            && !Physics.Raycast(transform.position, dir, out _, maxLockDistance, ~LockOnMask);
    }
    private void SwitchTarget(int direction)
    {
        if (!lockOn) return;
        if (potentialTargets.Count == 0 || currentLockable == null) return;

        // Получаем текущую позицию блокировки
        Vector3 currentLockPosition = currentLockable.GetLockOnTarget(transform).position;

        // Вычисляем горизонтальный угол текущей цели
        float currentAngle = GetHorizontalAngle(currentLockPosition);

        // Фильтруем и сортируем потенциальные цели
        var targetsWithAngles = potentialTargets
            .Where(t => t != currentLockable) // Исключаем текущую цель
            .Select(t => new
            {
                Target = t,
                Position = t.GetLockOnTarget(transform).position,
                Angle = GetHorizontalAngle(t.GetLockOnTarget(transform).position)
            })
            .Where(t =>
                // Выбираем цели в нужном направлении
                (direction > 0 && t.Angle > currentAngle) ||
                (direction < 0 && t.Angle < currentAngle)
            )
            .OrderBy(t => Mathf.Abs(t.Angle - currentAngle))
            .ToList();

        // Выбираем ближайшую цель
        if (targetsWithAngles.Count > 0)
        {
            var nearestTarget = targetsWithAngles.First().Target;

            // Обновляем текущую цель и камеру
            currentLockable = nearestTarget;
            FollowCinemachine.LookAt = currentLockable.GetLockOnTarget(transform);
        }
    }
    private float GetHorizontalAngle(Vector3 targetPos)
    {
        Vector3 dirToTarget = targetPos - transform.position;
        Vector3 flatDirToTarget = new Vector3(dirToTarget.x, 0, dirToTarget.z).normalized;

        // Используем SignedAngle для правильного определения направления
        return Vector3.SignedAngle(transform.forward, flatDirToTarget, Vector3.up);
    }
    #endregion
    private void DisableLockOn()
    {
        FollowCinemachine.LookAt = null;
        lockOn = false;
        currentLockable = null;
    }
    private void FindLockableTarget()
    {
        potentialTargets.Clear();
        Collider[] hits = Physics.OverlapSphere(transform.position, maxLockDistance, LockOnMask);
        foreach (Collider hit in hits)
            if (hit.TryGetComponent(out ILockable target))
                potentialTargets.Add(target);
    }
    #endregion

    #region Move
    private void Movement()
    {
        moveAmount = Mathf.Clamp01(Mathf.Abs(inputMoveDirection.y) + Mathf.Abs(inputMoveDirection.x));

        Vector3 movementDirection = camTransform.right * inputMoveDirection.x;
        movementDirection += camTransform.forward * inputMoveDirection.y;
        movementDirection.Normalize();

        Vector3 targetVelocity;

        //HANDLE ROTATION
        if (!animHook.isInteracting)// || animatorHook.canRotate)
        {
            Vector3 rotationDir = movementDirection;

            if (lockOn && !isSprint)
                rotationDir = currentLockable.GetLockOnTarget(transform).position - transform.position;

            HandleRotation(rotationDir);
        }

        if (lockOn && !isSprint)
        {
            targetVelocity = movementSpeed * inputMoveDirection.y * transform.forward;
            targetVelocity += movementSpeed * inputMoveDirection.x * transform.right;
        }
        else
        {
            currentSpeed = movementSpeed;
            if (isSprint)
            {
                if (movementDirection == Vector3.zero)
                    isSprint = false;
                currentSpeed = sprintSpeed;
            }

            targetVelocity = movementDirection * currentSpeed;
        }

        if (animHook.isInteracting)
            targetVelocity = deltaPosition * 1;

        //HANDLE MOVEMENT
        if (isGrounded)
        {
            Vector3 currentNormal = hit.normal;
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, currentNormal);

            rb.linearVelocity = targetVelocity;

            Vector3 grondedPosition = transform.position;
            grondedPosition.y = currentPosition.y;
            transform.position = Vector3.Lerp(transform.position, grondedPosition, Time.deltaTime / .1f);

            HandleAnimations();
        }
    }
    private void DeltaPosition(Vector3 deltaPosition)
    {
        if (animHook.isInteracting == false)
            return;

        if (isGrounded && Time.deltaTime > 0)
            this.deltaPosition = deltaPosition;
    }

    #region rotation
    private void HandleRotation(Vector3 targetDir)
    {
        float moveOvveride = moveAmount;

        if (lockOn)
            moveOvveride = 1;

        targetDir.Normalize();
        targetDir.y = 0;
        if (targetDir == Vector3.zero)
            targetDir = transform.forward;

        float actualRotationSpeed = rotationSpeed;
        if (animHook.isInteracting)
            actualRotationSpeed = atackRotationSpeed;

        Quaternion tr = Quaternion.LookRotation(targetDir);
        Quaternion targetRotation = Quaternion.Slerp(
            transform.rotation, tr,
            Time.deltaTime * moveOvveride * actualRotationSpeed);

        transform.rotation = targetRotation;
    }
    #endregion
    #endregion

    #region CheckGround
    private void CheckGround()
    {
        Vector3 origin = transform.position;
        origin.y += .5f;

        float dis = .4f;
        if (isOnAir)
            dis = .5f;

        Debug.DrawRay(origin, Vector3.down * dis, Color.red);
        if (Physics.SphereCast(origin, .2f, Vector3.down, out hit, dis, groundCheckMask))
        {
            isGrounded = true;
            currentPosition = hit.point;
            if (hit.point.y - transform.position.y < .5f)
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
    #endregion
    #region animation
    private void HandleAnimations()
    {
        animHook.anim.SetBool("isSprint", isSprint);

        float f = currentSpeed;
        if (moveAmount < .1f)
            f = 0;

        if (lockOn && !isSprint)
        {
            float ver = 0;
            float hor = 0;

            if(f > 0)
            {
                if(inputMoveDirection.y != 0)
                    if (inputMoveDirection.y > 0)
                        ver = currentSpeed;
                    else
                        ver = -currentSpeed;

                if(inputMoveDirection.x != 0)
                    if (inputMoveDirection.x > 0)
                        hor = currentSpeed;
                    else
                        hor = -currentSpeed;
            }

            animHook.anim.SetFloat("forward", ver, .2f, Time.deltaTime);
            animHook.anim.SetFloat("sideways", hor, .2f, Time.deltaTime);
        }
        else
        {
            animHook.anim.SetFloat("forward", f, .1f, Time.deltaTime);
            animHook.anim.SetFloat("sideways", 0);
        }
    }
    #endregion

}