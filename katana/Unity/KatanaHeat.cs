using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Нагрев катаны от блоков: копит «тепло» (0..1) при удачных блоках,
/// остывает со временем и пишет значение в шейдер Katana/HeatBlade через
/// MaterialPropertyBlock (материал при этом не инстанцируется).
///
/// Минимальное подключение:
///   1. Повесить компонент на корень катаны.
///   2. В Blade Renderers положить рендереры клинка (если пусто — соберутся сами).
///   3. Из своей боевой системы вызвать NotifyBlock(impact01, perfect).
///
/// Всё остальное (свет, искры, события) — опционально.
/// </summary>
[AddComponentMenu("Katana/Katana Heat")]
[DisallowMultipleComponent]
public sealed class KatanaHeat : MonoBehaviour
{
    public enum HeatStage
    {
        Cold = 0,      // холодная сталь
        Warm = 1,      // лёгкий нагрев, соломенный оттенок
        Tempered = 2,  // побежалость, фиолетово-синий
        Glowing = 3,   // кромка светится
        WhiteHot = 4,  // белое каление
    }

    [Serializable] public sealed class FloatEvent : UnityEvent<float> { }
    [Serializable] public sealed class IntEvent : UnityEvent<int> { }

    [Header("Цель")]
    [Tooltip("Рендереры клинка. Если оставить пустым — соберутся из детей автоматически.")]
    [SerializeField] private Renderer[] bladeRenderers;

    [Header("Нагрев")]
    [Tooltip("Прибавка тепла за один блок при impact = 1 и полностью холодном клинке.")]
    [SerializeField, Range(0.01f, 0.5f)] private float heatPerBlock = 0.10f;

    [Tooltip("Минимальная доля прибавки при самом слабом ударе (impact = 0).")]
    [SerializeField, Range(0f, 1f)] private float minImpactScale = 0.35f;

    [Tooltip("Идеальный блок (парирование) греет сильнее.")]
    [SerializeField, Range(1f, 4f)] private float perfectBlockMultiplier = 1.8f;

    [Tooltip("Чем горячее клинок, тем хуже он принимает тепло: 0 — без сопротивления, 1 — почти не греется.")]
    [SerializeField, Range(0f, 0.9f)] private float heatResistance = 0.55f;

    [Header("Серия блоков")]
    [Tooltip("Удары чаще этого интервала считаются серией и дают бонус к нагреву.")]
    [SerializeField, Range(0.1f, 2f)] private float comboWindow = 0.7f;

    [SerializeField, Range(0f, 0.5f)] private float comboBonusPerHit = 0.15f;

    [SerializeField, Range(1f, 3f)] private float comboMaxMultiplier = 1.7f;

    [Header("Остывание")]
    [SerializeField] private bool coolingEnabled = true;

    [Tooltip("Базовое остывание в единицах тепла в секунду (на холодном клинке).")]
    [SerializeField, Range(0f, 0.5f)] private float coolingPerSecond = 0.06f;

    [Tooltip("Доля остывания, которая растёт вместе с температурой (горячее остывает быстрее).")]
    [SerializeField, Range(0f, 1f)] private float coolingHeatFactor = 0.75f;

    [Tooltip("Множитель остывания: дождь, холодная локация, ветер и т.п.")]
    [SerializeField, Range(0f, 5f)] private float ambientCoolingMultiplier = 1f;

    [Header("Пороги стадий (для событий и UI)")]
    [SerializeField] private float[] stageThresholds = { 0.22f, 0.45f, 0.68f, 0.90f };

    [Header("Свет (опционально)")]
    [SerializeField] private Light heatLight;

    [Tooltip("Яркость света как функция тепла. По X — 0..1 тепла.")]
    [SerializeField] private AnimationCurve heatLightIntensity = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.5f, 0.12f),
        new Keyframe(0.8f, 1.1f), new Keyframe(1f, 2.6f));

    [SerializeField] private Gradient heatLightColor = CreateDefaultHeatGradient();

    [Tooltip("Дрожание света на высоких температурах.")]
    [SerializeField, Range(0f, 1f)] private float lightFlicker = 0.25f;

    [Header("Частицы (опционально)")]
    [Tooltip("ParticleSystem, который выплёвывает искры при блоке (Emit по количеству).")]
    [SerializeField] private ParticleSystem sparkBurst;

    [Tooltip("ParticleSystem с угольками/дымом над клинком. Его rateOverTime будет вести тепло.")]
    [SerializeField] private ParticleSystem embers;

    [SerializeField] private AnimationCurve emberRate = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.5f, 0f),
        new Keyframe(0.75f, 18f), new Keyframe(1f, 60f));

    [Header("События")]
    [Tooltip("Вызывается при каждом изменении тепла (значение 0..1).")]
    [SerializeField] private FloatEvent onHeatChanged = new FloatEvent();

    [Tooltip("Вызывается при блоке: сила удара 0..1. Удобно для VFX/SFX.")]
    [SerializeField] private FloatEvent onBlocked = new FloatEvent();

    [Tooltip("Сменилась стадия нагрева (0..4). Для UI и звука «клик».")]
    [SerializeField] private IntEvent onStageChanged = new IntEvent();

    private static readonly int HeatPropertyId = Shader.PropertyToID("_Heat");
    private static readonly string[] StageNames =
    {
        "Холодная сталь", "Нагрев", "Побежалость", "Раскалённый клинок", "Белое каление"
    };

    private MaterialPropertyBlock _propertyBlock;
    private float _heat;
    private float _lastApplied = -1f;
    private float _lastBlockTime = -999f;
    private int _comboHits;
    private int _stage = -1;

    /// <summary>Текущее тепло 0..1.</summary>
    public float Heat => _heat;

    /// <summary>Текущая стадия нагрева.</summary>
    public HeatStage Stage => (HeatStage)StageIndex;

    /// <summary>Текущая стадия числом 0..4.</summary>
    public int StageIndex
    {
        get
        {
            int stage = 0;
            if (stageThresholds != null)
            {
                for (int i = 0; i < stageThresholds.Length && i < 4; i++)
                    if (_heat >= stageThresholds[i]) stage = i + 1;
            }
            return stage;
        }
    }

    /// <summary>Человекочитаемое имя стадии (для UI).</summary>
    public string StageName => StageNames[Mathf.Clamp(StageIndex, 0, StageNames.Length - 1)];

    /// <summary>Клинок на максимуме.</summary>
    public bool IsWhiteHot => _heat >= 0.995f;

    /// <summary>Сколько секунд прошло с последнего блока.</summary>
    public float TimeSinceLastBlock => Time.time - _lastBlockTime;

    /// <summary>Множитель серии, который получил бы следующий блок.</summary>
    public float CurrentComboMultiplier =>
        Mathf.Min(1f + comboBonusPerHit * Mathf.Max(0, _comboHits - 1), comboMaxMultiplier);

    private void Awake()
    {
        if (bladeRenderers == null || bladeRenderers.Length == 0)
            RefreshRenderers();

        _propertyBlock = new MaterialPropertyBlock();
        ApplyHeat(force: true);
    }

    private void OnEnable()
    {
        ApplyHeat(force: true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (Time.time - _lastBlockTime > comboWindow)
            _comboHits = 0;

        if (coolingEnabled && _heat > 0f)
        {
            // Ньютоновское остывание: скорость зависит и от температуры, и от среды.
            float rate = coolingPerSecond * Mathf.Lerp(1f, 1f + coolingHeatFactor, _heat);
            _heat = Mathf.Max(0f, _heat - rate * ambientCoolingMultiplier * dt);
        }

        ApplyHeat(force: false);
        UpdateHeatLight();
        UpdateEmbers();
    }

    /// <summary>
    /// Основной вход из боя. Вызывайте, когда удар врага пришёлся в блок.
    /// </summary>
    /// <param name="impact01">Сила удара: 0 — чиркнуло, 1 — тяжёлый удар.</param>
    /// <param name="perfect">Идеальный блок / парирование.</param>
    public void NotifyBlock(float impact01 = 1f, bool perfect = false)
    {
        float now = Time.time;
        impact01 = Mathf.Clamp01(impact01);

        _comboHits = now - _lastBlockTime <= comboWindow ? _comboHits + 1 : 1;
        _lastBlockTime = now;

        float combo = Mathf.Min(1f + comboBonusPerHit * Mathf.Max(0, _comboHits - 1),
                                comboMaxMultiplier);
        float impactScale = Mathf.Lerp(minImpactScale, 1f, impact01);
        float gain = heatPerBlock * impactScale * combo * (1f - _heat * heatResistance);
        if (perfect) gain *= perfectBlockMultiplier;

        _heat = Mathf.Clamp01(_heat + gain);

        ApplyHeat(force: true);
        onBlocked.Invoke(impact01);

        if (sparkBurst != null)
        {
            int count = Mathf.RoundToInt(Mathf.Lerp(4f, 28f, impact01) * (perfect ? 1.5f : 1f));
            sparkBurst.Emit(count);
        }
    }

    /// <summary>Добавить тепло напрямую (костёр, горящий враг, скриптовое событие).</summary>
    public void AddHeat(float normalizedAmount)
    {
        if (normalizedAmount <= 0f) return;
        _heat = Mathf.Clamp01(_heat + normalizedAmount);
        ApplyHeat(force: true);
    }

    /// <summary>Принудительно остудить (окунуть в воду, дождь, смена оружия).</summary>
    public void Cool(float normalizedAmount)
    {
        if (normalizedAmount <= 0f) return;
        _heat = Mathf.Max(0f, _heat - normalizedAmount);
        ApplyHeat(force: true);
    }

    /// <summary>Полностью остудить клинок.</summary>
    public void ResetHeat()
    {
        _heat = 0f;
        _comboHits = 0;
        _lastBlockTime = -999f;
        ApplyHeat(force: true);
    }

    /// <summary>Пересобрать список рендереров клинка (после смены модели/скина).</summary>
    public void RefreshRenderers()
    {
        var found = new List<Renderer>(4);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                continue;
            if (r.sharedMaterial == null || !r.sharedMaterial.HasProperty(HeatPropertyId))
                continue;
            found.Add(r);
        }
        bladeRenderers = found.ToArray();
    }

    private void ApplyHeat(bool force)
    {
        if (!force && Mathf.Abs(_heat - _lastApplied) < 0.0005f)
            return;

        _lastApplied = _heat;

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        // Один блок на все рендереры: переопределяем только _Heat,
        // остальные свойства берутся из материала.
        _propertyBlock.Clear();
        _propertyBlock.SetFloat(HeatPropertyId, _heat);

        if (bladeRenderers != null)
        {
            for (int i = 0; i < bladeRenderers.Length; i++)
            {
                Renderer r = bladeRenderers[i];
                if (r != null) r.SetPropertyBlock(_propertyBlock);
            }
        }

        onHeatChanged.Invoke(_heat);

        int newStage = StageIndex;
        if (newStage != _stage)
        {
            _stage = newStage;
            onStageChanged.Invoke(_stage);
        }
    }

    private void UpdateHeatLight()
    {
        if (heatLight == null) return;

        float baseIntensity = Mathf.Max(0f, heatLightIntensity.Evaluate(_heat));
        float flicker = 1f;
        if (lightFlicker > 0f && _heat > 0.35f)
        {
            float n = Mathf.PerlinNoise(Time.time * 9.3f, 0.37f);
            flicker = 1f + (n - 0.5f) * 2f * lightFlicker * Mathf.InverseLerp(0.35f, 1f, _heat);
        }

        heatLight.intensity = baseIntensity * flicker;
        heatLight.color = heatLightColor.Evaluate(Mathf.Clamp01(_heat));
        heatLight.enabled = heatLight.intensity > 0.001f;
    }

    private void UpdateEmbers()
    {
        if (embers == null) return;

        var emission = embers.emission;
        float rate = Mathf.Max(0f, emberRate.Evaluate(_heat));
        emission.rateOverTimeMultiplier = rate;
        emission.enabled = rate > 0.01f;

        if (!embers.isPlaying && rate > 0.01f)
            embers.Play();
    }

    private static Gradient CreateDefaultHeatGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.12f, 0.02f), 0.0f),
                new GradientColorKey(new Color(1.00f, 0.35f, 0.05f), 0.55f),
                new GradientColorKey(new Color(1.00f, 0.82f, 0.45f), 1.0f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            });
        return gradient;
    }

    // --- удобные кнопки в контекстном меню компонента -----------------------
    [ContextMenu("Тест: обычный блок")]
    private void TestBlock() => NotifyBlock(0.7f, false);

    [ContextMenu("Тест: идеальный блок")]
    private void TestPerfectBlock() => NotifyBlock(1f, true);

    [ContextMenu("Тест: серия из 5 блоков")]
    private void TestCombo()
    {
        for (int i = 0; i < 5; i++) NotifyBlock(0.8f, false);
    }

    [ContextMenu("Тест: остудить")]
    private void TestReset() => ResetHeat();

    private void OnValidate()
    {
        heatPerBlock = Mathf.Clamp(heatPerBlock, 0.01f, 0.5f);
        coolingPerSecond = Mathf.Max(0f, coolingPerSecond);
        _heat = Mathf.Clamp01(_heat);

        if (stageThresholds != null)
            for (int i = 0; i < stageThresholds.Length; i++)
                stageThresholds[i] = Mathf.Clamp01(stageThresholds[i]);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (bladeRenderers == null) return;
        Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.6f);
        foreach (var r in bladeRenderers)
        {
            if (r != null) Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
    }
#endif
}
