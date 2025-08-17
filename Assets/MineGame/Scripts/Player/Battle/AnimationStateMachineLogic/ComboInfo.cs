using UnityEngine;

public class ComboInfo : StateMachineBehaviour
{
    [SerializeField] private ComboModel[] combos;
    private PlayerView playerView;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerView ??= animator.GetComponentInParent<PlayerView>();

        playerView.comboController.LoadCombo(combos);
    }
}