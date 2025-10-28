using UnityEngine;

[System.Serializable]
public class MotionWarpSettings
{
    public bool warpPosition = true;
    public bool warpRotation = true;
    public bool ignoreVertical = false;
    public float desiredDistance = 0f;

    [Header("Ограничения")]
    public float maxWarpDistance = 3f;
}
