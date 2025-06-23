using System;
using UnityEngine;

public class AnimatorHookView : MonoBehaviour
{
    #region param
    [HideInInspector] public Animator anim;
    [HideInInspector] public bool isInteracting;

    [HideInInspector] public bool canRotate;

    [HideInInspector] public bool canMove;

    [HideInInspector] public bool canDoCombo;

    [HideInInspector] public bool openDamageCollider;

    [HideInInspector] public bool hasLockTarget;

    public event Action<Vector3> DeltaPositionAnimator;
    #endregion

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void OnAnimatorMove() =>
        OnAnimatorMoveOvveride();

    private void OnAnimatorMoveOvveride() =>
        DeltaPositionAnimator?.Invoke(anim.deltaPosition / Time.deltaTime);

    public void PlayTargetAnimation(string targetAnim, bool isInteracting)
    {
        anim.SetBool("isInteracting", isInteracting);
        anim.CrossFade(targetAnim, 0.2f);
        this.isInteracting = isInteracting;
    }

    public void OpenCanMove()
    {
        canMove = true;
    }

    public void OpenDamageCollider()
    {
        openDamageCollider = true;
    }
    public void CloseDamageCollider()
    {
        openDamageCollider = false;
    }

    public void EnableCombo()
    {
        canDoCombo = true;
    }

    public void EnabledRotation()
    {
        canRotate = true;
    }
    public void DisableRotation()
    {
        canRotate = false;
    }
}
