using UnityEngine;

public class OnEnterDisableCombo : StateMachineBehaviour
{
    override public void OnStateEnter(Animator anim, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerView.animHook.canDoCombo = false;
    }
}
