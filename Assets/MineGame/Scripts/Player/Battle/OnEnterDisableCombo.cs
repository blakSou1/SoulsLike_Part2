using UnityEngine;

public class OnEnterDisableCombo : StateMachineBehaviour
{
    private PlayerView playerView;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerView ??= animator.GetComponentInParent<PlayerView>();

        playerView.animHook.canDoCombo = false;
    }
}
