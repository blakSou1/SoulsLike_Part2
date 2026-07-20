using UnityEngine;

public class OnEnterDisableCombo : StateMachineBehaviour
{
    private PlayerView playerView;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerView ??= animator.GetComponentInParent<PlayerView>();

        if (playerView == null) return;

        playerView.AnimHook.canDoCombo = false;
    }
}
