using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Battle/Profile")]
public class SetMoveProfile : ScriptableObject
{
    [Header("Аниматор контролер который будет висеть на игроке")]
    public AnimatorController animationController;

    [Header("Список кнопок ввода")]
    /// <summary>
    /// получаем список действий за определенной кнопкой ввода
    /// </summary>
    public List<InputList> atackInputs;
}

[System.Serializable]
public class InputList
{
    [Header("Кнопки ввода")]
    /// <summary>
    /// кнопка ввода
    /// </summary>
    public InputActionReference atackInputs;

    [Header("список состояния ввода")]
    public List<StateAction> inputStatsAction;
}

[System.Serializable]
public class StateAction
{
    [Header("Состояние ввода")]
    /// <summary>
    /// состояние ввода
    /// </summary>
    public InputActionPhase inputsPhase;

    [Header("Воспроизводимая анмиация")]
    public ItemActionContainerModel inputStatsAction;
}

[System.Serializable]
public class ItemActionContainerModel
{
    [Header("Название воспроизводимой анимации")]
    public string animName;

    [Header("блокирует ли эта анимация управление игрока? да-нет")]
    public bool isInteracting = true;
}