using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class TimedBoolInputBuffer : TimedInputBuffer<bool>
{
    public void Set() => base.Set(true);
    public bool TryConsume() => base.TryConsume(out _);
}

[Serializable]
public class TimedIntInputBuffer : TimedInputBuffer<int>
{
}

[Serializable]
public class TimedInputActionInputBuffer: TimedInputBuffer<BufferedInputData>
{
}


[Serializable]
public class TimedInputBuffer<T>
{
    [SerializeField, Min(0f)] float holdTime = 0.12f;
    [SerializeField] T bufferredValue;

    float expiryTime = float.NegativeInfinity;
    bool hasBufferedInput;

    public bool HasBuffer => hasBufferedInput && Time.time <= expiryTime;
    public float HoldTime => holdTime;
    public T Value => bufferredValue;

    public void Set(T value)
    {
        bufferredValue = value;
        hasBufferedInput = true;
        expiryTime = Time.time + holdTime;
    }

    public void Consume()
    {
        hasBufferedInput = false;
        expiryTime = float.NegativeInfinity;
        bufferredValue = default;
    }

    public bool TryConsume(out T value)
    {
        if (!HasBuffer)
        {
            value = default;
            return false;
        }

        value = bufferredValue;
        Consume();
        return true;
    }

    public void SetHoldTime(float seconds) => holdTime = Mathf.Max(0f, seconds);
}

public class BufferedInputData
{
    public string actionName;
    public InputActionPhase phase;

    public BufferedInputData(InputAction.CallbackContext context)
    {
        actionName = context.action.name;
        phase = context.phase;
    }
}
