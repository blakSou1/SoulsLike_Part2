using UnityEngine;

public class ComboInfo : StateMachineBehaviour
{
    [SerializeField] private ComboModel[] combos;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerView.comboController.LoadCombo(combos);
    }
}