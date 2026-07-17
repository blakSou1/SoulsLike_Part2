using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorHookView : MonoBehaviour
{
	public Animator Anim { get; private set; }

    [ReadOnly] public bool isInteracting;
    [ReadOnly] public bool isInterrupt;
	[ReadOnly] public bool canDoCombo;

    [ReadOnly] public bool canRotate;
	[ReadOnly] public bool canMove;
	[ReadOnly] public bool openDamageCollider;

	public event Action<Vector3> DeltaPositionAnimator;

    public event Action IOpenDamageCollider;
	public event Action ICloseDamageCollider;

    private void Start()
	{
        Anim = GetComponent<Animator>();
	}

	public void OnAnimatorMove() =>
		OnAnimatorMoveOvveride();

	private void OnAnimatorMoveOvveride() =>
		DeltaPositionAnimator?.Invoke(Anim.deltaPosition / Time.deltaTime);

	public void PlayTargetAnimation(string targetAnim, bool isInteracting)
	{
		Anim.SetBool("isInteracting", isInteracting);
		Anim.CrossFade(targetAnim, 0.2f);
		this.isInteracting = isInteracting;
		canMove = false;
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
		IOpenDamageCollider?.Invoke();
    }
	public void CloseDamageCollider()
	{
		openDamageCollider = false;
		ICloseDamageCollider?.Invoke();
    }
    #endregion

    #region Combo
    public void EnableCombo()
    {
        canDoCombo = true;
    }
    public void DisableCombo()
    {
        canDoCombo = false;
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

    public void EnabledInterrupt()
    {
        isInterrupt = true;
    }
    public void DisableInterrupt()
    {
        isInterrupt = false;
    }

    #endregion
}
