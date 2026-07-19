using System;
using UnityEngine;

public class AnimationFinished : MonoBehaviour
{
    public string stateName = "Finisher";
    private string nameAnimInState;

    [NonSerialized] public Enemy enemy;

    [Header("player atack")]
    [SerializeField] private AnimationClip front;
    [SerializeField] private AnimationClip Back;
    [SerializeField] private AnimationClip Air;

    [Header("enemy Hit")]
    [SerializeField] private AnimationClip frontE;
    [SerializeField] private AnimationClip BackE;
    [SerializeField] private AnimationClip AirE;

    private AnimatorOverrideController overrideController;

    public void Init()
    {
        overrideController = enemy.AnimHook.OverrideController;
    }

    public AnimationClip GetAnimation()//TODO
    {
        return front;
    }

    public void StartAnimEnd() 
    {
        AnimationClip clip = frontE;

        //ItemActionContainerModel actionContainer = null;

        overrideController[nameAnimInState] = clip;
        nameAnimInState = clip.name;

        enemy.AnimHook.Anim.Rebind();
        enemy.AnimHook.Anim.Update(0f);

        enemy.AnimHook.PlayTargetAnimation(stateName, true);

        // TODO: Запустить анимацию добивания
        // TODO: Эффекты
    }
}
