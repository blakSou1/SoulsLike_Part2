using System;
using UnityEngine;

public class AnimationFinished : MonoBehaviour
{
    public string stateName = "Finisher";

    [NonSerialized] public Enemy enemy;

    [Header("player atack")]
    [SerializeField] private AnimatorOverrideController front;
    [SerializeField] private AnimatorOverrideController Back;
    [SerializeField] private AnimatorOverrideController Air;

    [Header("enemy Hit")]
    [SerializeField] private AnimatorOverrideController frontE;
    [SerializeField] private AnimatorOverrideController BackE;
    [SerializeField] private AnimatorOverrideController AirE;

    public AnimatorOverrideController GetAnimation()//TODO
    {
        return front;
    }

    public void StartAnimEnd() 
    {
        AnimatorOverrideController clip = frontE;

        //ItemActionContainerModel actionContainer = null;

        enemy.AnimHook.Anim.runtimeAnimatorController = clip;

        enemy.AnimHook.PlayTargetAnimation(stateName, true);

        // TODO: Запустить анимацию добивания
        // TODO: Эффекты
    }
}
