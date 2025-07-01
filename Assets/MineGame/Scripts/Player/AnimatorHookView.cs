using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorHookView : MonoBehaviour
{
    #region Param
    #region Components
    [HideInInspector] public Animator anim;
    #endregion

    #region Stats
    public bool isInteracting;

    public bool canRotate;

    public bool canMove;

    public bool canDoCombo;

    public bool openDamageCollider;
    #endregion

    #region Events
    public event Action katanaIsHook;
    public event Action<Vector3> DeltaPositionAnimator;
    #endregion
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

    #region AnimationEvent
    #region Move
    public void OpenCanMove()
    {
        canMove = true;
    }
    #endregion

    #region DamageCollider
    public void OpenDamageCollider()
    {
        openDamageCollider = true;
    }
    public void CloseDamageCollider()
    {
        openDamageCollider = false;
    }
    #endregion

    #region Combo
    public void EnableCombo()
    {
        canDoCombo = true;
    }
    #endregion

    #region IsKatana
    public void IsHookKatana()
    {
        katanaIsHook?.Invoke();
    }
    #endregion

    #region Rotation
    public void EnabledRotation()
    {
        canRotate = true;
    }
    public void DisableRotation()
    {
        canRotate = false;
    }
    #endregion
    #endregion
}
