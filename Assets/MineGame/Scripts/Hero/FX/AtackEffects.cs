using UnityEngine;

public class AtackEffects : MonoBehaviour
{
    public void PlayAtackSlashFX(ItemActionContainerModel actionContainer)
    {
        if (actionContainer == null || actionContainer.prefabSlashFX == null)
            return;

        GameObject ob = Instantiate(actionContainer.prefabSlashFX, G.playerView.transform.GetChild(0).transform);

        ParticleSystem ps = ob.GetComponent<ParticleSystem>();
        var mainModule = ps.main;

        mainModule.startRotation3D = true;

        float yRotation = G.playerView.transform.eulerAngles.y * Mathf.Deg2Rad;

        mainModule.startRotationX = new ParticleSystem.MinMaxCurve(0);
        mainModule.startRotationY = new ParticleSystem.MinMaxCurve(yRotation);
        mainModule.startRotationZ = new ParticleSystem.MinMaxCurve(0);
    }
}
