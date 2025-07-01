using UnityEngine;

public class ScabbardHolder : MonoBehaviour
{
    public Transform IsKatana()
    {
        return transform.GetChild(0);
    }
}