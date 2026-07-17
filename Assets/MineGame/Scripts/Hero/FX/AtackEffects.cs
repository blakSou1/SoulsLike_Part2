using UnityEngine;

public class AtackEffects : MonoBehaviour
{
    public void PlayAtackFX(ItemActionContainerModel actionContainer)
    {
        if (actionContainer == null || actionContainer.prefabFX == null)
            return;

        GameObject ob = Instantiate(actionContainer.prefabFX, G.playerView.healthController.weapon.transform);
    }
}
