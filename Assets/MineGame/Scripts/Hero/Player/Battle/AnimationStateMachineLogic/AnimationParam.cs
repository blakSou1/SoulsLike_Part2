using UnityEngine;

public class AnimationParam : StateMachineBehaviour
{
    [SerializeField] private AtackModel inputStatsAction;
    private PlayerView playerView;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerView ??= animator.GetComponentInParent<PlayerView>();

        playerView.ComboController.LoadAtackParam(inputStatsAction.inputStatsAction);
    }
}
