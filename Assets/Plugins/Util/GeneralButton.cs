using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GeneralButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TextMeshProUGUI label;

    [SerializeField] private Color labelDefolt = new(1f, 1f, 1f, 1f);
    [SerializeField] private Color labelHover = new(1f, 1f, 1f, 1f);
    [SerializeField] private Color labelPressed = new(1f, 1f, 1f, 1f);

    [SerializeField] private GameObject stateDefolt;
    [SerializeField] private GameObject stateHover;
    [SerializeField] private GameObject statePressed;

    public UnityEvent onClick;
    public bool interactable { get; private set; } = true;

    private void Awake()
    {
        if (interactable)
            State(stateDefolt);
        else
            State(statePressed);
    }
    private void OnDestroy()
    {
        State(stateDefolt);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (interactable)
            State(stateHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (interactable)
            State(stateDefolt);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (interactable)
            onClick?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable)
            State(statePressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (interactable)
            State(stateDefolt);
    }

    private void State(GameObject go)
    {
        stateDefolt.SetActive(stateDefolt == go);
        stateHover.SetActive(stateHover == go);
        statePressed.SetActive(statePressed == go);

        if (label == null) return;
        if (stateHover.activeSelf)
            label.color = labelHover;
        else if (statePressed.activeSelf)
            label.color = labelPressed;
        else
            label.color = labelDefolt;
    }

    public void UpdateInteractable(bool newState)
    {
        interactable = newState;
        if (interactable)
            State(stateDefolt);
        else
            State(statePressed);
    }
}