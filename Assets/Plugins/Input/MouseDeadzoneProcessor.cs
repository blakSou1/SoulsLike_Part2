using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseDeadzoneProcessor : InputProcessor<float> {
    public float min = 0.3f;
    
    public override float Process(float value, InputControl control) {
        return Mathf.Abs(value) >= min ? Mathf.Sign(value) : 0f;
    }
}

// Регистрация процессора
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class MouseDeadzoneRegister {
    static MouseDeadzoneRegister() {
        InputSystem.RegisterProcessor<MouseDeadzoneProcessor>();
    }
}