using UnityEngine;

[System.Serializable]
public class MotionWarpSettings
{
    public bool warpPosition = true;
    public bool warpRotation = true;
    public bool ignoreVertical = false;

    [Header("Distance Settings")]
    public float desiredDistance = 0f;
    [Tooltip("Максимальная дистанция, на которой активируется варп (если цель дальше - варп не начнется)")]
    public float activationDistance = 10f;
    
    [Header("Ограничения")]
    public float maxWarpDistance = 3f;

    [Header("Finished Settings")]
    public bool snapToFinish = false;

    [Tooltip("Угол поворота героя относительно противника:\n" +
         "0°  - лицом к противнику\n" +
         "90° - противник справа\n" +
         "-90° - противник слева\n" +
         "180° - спиной к противнику")]
    public float targetRotationOffset = 0f;

}
