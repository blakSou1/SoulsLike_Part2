using UnityEngine;
using VContainer;

public class PlayerView : MonoBehaviour
{
#region param
    [Inject] private PlayerInput input;

    private bool isInteracting;
    private bool lockOn;

#region move
    private bool isSprint;
    private float moveAmount;
    [SerializeField] private readonly float movementSpeed = 3f;
    [SerializeField] private readonly float sprintSpeed = 9f;
    private Vector2 currentPosition;
    private Vector3 inputMoveDirection;
#endregion
#region rotation
    [SerializeField] private readonly float rotationSpeed = 10f;
    [SerializeField] private readonly float atackRotationSpeed = 10f;
#endregion

    private Transform lockTarget;
    private Transform camTransform;


#region component
    private Rigidbody rb;
    private Animator anim;
#endregion
#region GroundCheck
    private bool isOnAir;
    public bool isGrounded;
    private RaycastHit hit;
    [SerializeField] private LayerMask ignireForGroundCheck;
#endregion
#endregion

    private void Start(){
        camTransform = Camera.main.transform;
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

#region input
        input.Player.Move.performed += i => inputMoveDirection = i.ReadValue<Vector2>();
        input.Player.Move.canceled += i => inputMoveDirection = Vector2.zero;

#endregion
    }

    private void FixedUpdate()
    {
        CheckGround();
        Movement();
    }

#region Move
    private void Movement(){
        moveAmount = Mathf.Clamp01(Mathf.Abs(inputMoveDirection.y) + Mathf.Abs(inputMoveDirection.x));

        Vector3 movementDirection = camTransform.right * inputMoveDirection.x;
        movementDirection += camTransform.forward * inputMoveDirection.y;
        movementDirection.Normalize();
        
        Vector3 targetVelocity;

            //HANDLE ROTATION
            if (!isInteracting)// || animatorHook.canRotate)
            {
                Vector3 rotationDir = movementDirection;

                if (lockOn && !isSprint)
                    rotationDir = lockTarget.position - transform.position;

                HandleRotation(rotationDir);
            }

            if (lockOn && !isSprint)
            {
                targetVelocity = movementSpeed * inputMoveDirection.y * transform.forward;
                targetVelocity += movementSpeed * inputMoveDirection.x * transform.right;
            }
            else
            {
                float speed = movementSpeed;
                if (isSprint)
                {
                    if (movementDirection == Vector3.zero)
                        isSprint = false;
                    speed = sprintSpeed;
                }

                targetVelocity = movementDirection * speed;
            }

            //if (isInteracting)
            //    targetVelocity = deltaPosition * VelocityMultiplier;

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
    private void HandleAnimations()
    {
            // anim.SetBool("isSprint", isSprint);

            // float f = currentSpeed;
            // if (moveAmount < .1f)
            //     f = 0;

            // if (lockOn && !isSprint)
            // {
            //     float ver = 0;
            //     float hor = 0;

            //     if(f > 0)
            //     {
            //         if(inputMoveDirection.y != 0)
            //             if (inputMoveDirection.y > 0)
            //                 ver = currentSpeed;
            //             else
            //                 ver = -currentSpeed;

            //         if(inputMoveDirection.x != 0)
            //             if (inputMoveDirection.x > 0)
            //                 hor = currentSpeed;
            //             else
            //                 hor = -currentSpeed;
            //     }

            //     anim.SetFloat("forward", ver, .2f, Time.deltaTime);
            //     anim.SetFloat("sideways", hor, .2f, Time.deltaTime);
            // }
            // else
            // {
            //     anim.SetFloat("forward", f, .1f, Time.deltaTime);
            //     anim.SetFloat("sideways", 0);
            // }
    }

#region rotation
    private void HandleRotation(Vector3 targetDir){
            float moveOvveride = moveAmount;

            if (lockOn)
                moveOvveride = 1;

            targetDir.Normalize();
            targetDir.y = 0;
            if (targetDir == Vector3.zero)
                targetDir = transform.forward;

            float actualRotationSpeed = rotationSpeed;
            if (isInteracting)
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
    private void CheckGround(){
        Vector3 origin = transform.position;
        origin.y += .7f;

        float dis = .8f;
        if (isOnAir)
            dis = .6f;

        Debug.DrawRay(origin, Vector3.down * dis, Color.red);
        if (Physics.SphereCast(origin, .2f, Vector3.down, out hit, dis, ignireForGroundCheck))
        {
            isGrounded = true;
            currentPosition = hit.point;
            if(hit.point.y - transform.position.y < .5f)
                currentPosition.y = hit.point.y + .2f;
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
}
