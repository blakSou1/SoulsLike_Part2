using UnityEngine;

public class OnEnterDisableCombo : StateMachineBehaviour
{
    private PlayerView playerView;

#if UNITY_EDITOR
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerView ??= animator.GetComponentInParent<PlayerView>();

        playerView.AnimHook.canDoCombo = false;
    }
#endif
}
