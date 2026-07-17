using UnityEngine;

public class OnStateEnterBool : StateMachineBehaviour
{
    [Header("Название параметра аниматора")]
    public string boolName;
    [Header("устанавливается при входе в состояние, если включен ресет устанавливает противоположное при выходе")]
    public bool status;
    public bool resetOnExit;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(boolName, status);
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(resetOnExit)
            animator.SetBool(boolName, !status);
    }
}
