using UnityEngine;

public class MotionWarpState : StateMachineBehaviour
{
    public MotionWarpSettings warpSettings;
    private MotionWarpingSystem warpSystem;
    private bool initialized;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!initialized)
        {
            warpSystem = animator.GetComponent<MotionWarpingSystem>();
            initialized = true;
        }

        warpSystem.ApplyWarpSettings(warpSettings);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        warpSystem.StopWarpingAnimationEvent();
    }
}
