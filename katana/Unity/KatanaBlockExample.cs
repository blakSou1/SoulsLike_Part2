using UnityEngine;

/// <summary>
/// Пример того, как связать KatanaHeat с системой боя. Это не готовый
/// комбат-контроллер, а минимальная обвязка — замените тела методов вызовами
/// из вашей боевой системы.
///
/// Логика простая: удар греет клинок только тогда, когда игрок держит блок.
/// </summary>
[RequireComponent(typeof(KatanaHeat))]
[AddComponentMenu("Katana/Katana Block Example")]
public sealed class KatanaBlockExample : MonoBehaviour
{
    [SerializeField] private KatanaHeat heat;

    [Tooltip("Проверка блоком: обычно Mouse1 или отдельное состояние анимации.")]
    [SerializeField] private bool isBlocking;

    [Header("Демо (удалите в продакшене)")]
    [SerializeField] private KeyCode testBlockKey = KeyCode.B;
    [SerializeField, Range(0f, 1f)] private float testImpact = 0.7f;
    [SerializeField] private bool testPerfect;

    public bool IsBlocking => isBlocking;

    private void Reset() => heat = GetComponent<KatanaHeat>();

    private void Awake()
    {
        if (heat == null) heat = GetComponent<KatanaHeat>();
    }

    private void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(testBlockKey))
            OnIncomingAttack(testImpact, testPerfect);
#endif
    }

    /// <summary>Держите в актуальном состоянии из ввода/анимации.</summary>
    public void SetBlocking(bool value) => isBlocking = value;

    /// <summary>
    /// Вызывайте, когда атака врага попала в блок.
    /// </summary>
    /// <param name="impact01">0..1 — насколько тяжёлый удар.</param>
    /// <param name="perfect">Попал ли игрок в окно парирования.</param>
    public void OnIncomingAttack(float impact01, bool perfect = false)
    {
        if (!isBlocking)
            return; // удар прошёл в тело — клинок не греется

        heat.NotifyBlock(impact01, perfect);
    }

    // Альтернатива через физику: сложите на оружие врага слой/тег "Attack"
    // и Collider-триггер перед клинком игрока.
    private void OnTriggerEnter(Collider other)
    {
        if (!isBlocking) return;
        if (other.gameObject.tag != "Attack") return;

        // Грубая оценка силы удара по скорости оружия. Замените на свой урон.
        float speed = other.attachedRigidbody != null
            ? other.attachedRigidbody.velocity.magnitude
            : 0f;
        OnIncomingAttack(Mathf.Clamp01(speed / 12f), perfect: false);
    }
}
