using UnityEngine;

public class AnimationParam : StateMachineBehaviour
{
    [SerializeField] private AtackModel inputStatsAction;
    private PlayerView playerView;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerView ??= animator.GetComponentInParent<PlayerView>();

        if (playerView == null) return;

        playerView.ComboController.LoadAtackParam(inputStatsAction.inputStatsAction);
        playerView.motionWarpingSystem.ApplyWarpSettings(inputStatsAction.inputStatsAction);
    }
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerView ??= animator.GetComponentInParent<PlayerView>();

        if (playerView == null) return;

        playerView.ComboController.LoadAtackParam(null);
        playerView.motionWarpingSystem.ApplyWarpSettings(null);
    }
}
