using UnityEngine;

public class HolderInHand : MonoBehaviour
{
    public Transform IsKatana()
    {
        return transform.GetChild(0);
    }
}