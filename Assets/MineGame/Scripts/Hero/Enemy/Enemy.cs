using System;
using UnityEngine;

public class Enemy : MonoBehaviour, ILockable
{
    public AnimatorHookView AnimHook { get; private set; }
    [NonSerialized] public MotionWarpingSystem motionWarpingSystem;

    public Transform LockOnTarget;
    [NonSerialized] public AnimationFinished animationFinished;

    private void Start()
    {
        AnimHook = GetComponentInChildren<AnimatorHookView>();
        motionWarpingSystem = GetComponentInChildren<MotionWarpingSystem>();
        motionWarpingSystem.currentTarget = G.playerView.transform;

        animationFinished = GetComponentInChildren<AnimationFinished>();
        animationFinished.enemy = this;
    }

    public Transform GetLockOnTarget()
    {
        return LockOnTarget;
    }

    public bool IsAlive()
    {
        throw new System.NotImplementedException();
    }

}
