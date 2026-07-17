using UnityEngine;

public class InputBufferLockOnExample : MonoBehaviour, IService
{
    public void Init()
    {
        LockOnInputSubscription();
    }

    private void OnDestroy()
    {
        LockOnInputUnsubscription();
    }

    private void LockOnInputSubscription()
    {
        G.inputs.Player.LockOn.started += i => G.playerView.LockOnComponent.LockOn();
        G.inputs.Player.NewTargetLock.started += i => {
            float inputValue = i.ReadValue<float>();
            int deadZone = 70;

            if (Mathf.Abs(inputValue) > deadZone)
            {
                int direction = inputValue > 0 ? 1 : -1;
                G.playerView.LockOnComponent.SwitchTarget(direction);
            }
        }; 
    }

    private void LockOnInputUnsubscription()
    {
        G.inputs.Player.LockOn.started -= i => G.playerView.LockOnComponent.LockOn();
        G.inputs.Player.NewTargetLock.started -= i => {
            float inputValue = i.ReadValue<float>();
            int deadZone = 70;

            if (Mathf.Abs(inputValue) > deadZone)
            {
                int direction = inputValue > 0 ? 1 : -1;
                G.playerView.LockOnComponent.SwitchTarget(direction);
            }
        };
    }

}
