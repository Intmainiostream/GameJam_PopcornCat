using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// WorldToggle — Chromablind
/// Attach to a singleton GameManager or dedicated GameObject.
/// Press and hold Shift → switches to Monochrome world with animated background.
/// Release Shift        → returns to Color world.
///
/// Assign colorBg / monoBg in the Inspector, then tag your world-specific
/// GameObjects with "ColorOnly" or "MonoOnly" so this script can toggle them.
/// </summary>
public class WorldToggle : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Background Colors")]
    [SerializeField] private Color colorWorldBg  = new Color(0.102f, 0.086f, 0.188f); // dark indigo
    [SerializeField] private Color monoWorldBg   = new Color(0.102f, 0.102f, 0.102f); // near-black

    [Header("Transition")]
    [Tooltip("Seconds to fully transition between worlds")]
    [SerializeField] private float transitionDuration = 0.25f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Vignette Pulse (optional)")]
    [Tooltip("Assign a UI Image set to stretch-fill the canvas for a brief flash effect. Leave null to skip.")]
    [SerializeField] private UnityEngine.UI.Image vignetteImage;
    [SerializeField] private float vignettePeakAlpha = 0.18f;
    [SerializeField] private float vignetteDuration  = 0.12f;

    [Header("Camera Shake (optional)")]
    [SerializeField] private bool  shakeCameraOnToggle = true;
    [SerializeField] private float shakeMagnitude      = 0.06f;
    [SerializeField] private float shakeDuration       = 0.08f;

    [Header("World Object Tags")]
    [Tooltip("GameObjects with this tag are only active in Color world")]
    [SerializeField] private string colorOnlyTag = "ColorOnly";
    [Tooltip("GameObjects with this tag are only active in Mono world")]
    [SerializeField] private string monoOnlyTag  = "MonoOnly";

    // ── Public state ─────────────────────────────────────────────────────────
    public static WorldToggle Instance { get; private set; }
    public bool IsMonoWorld { get; private set; } = false;

    // ── Private ──────────────────────────────────────────────────────────────
    private Camera   _cam;
    private Coroutine _transitionCoroutine;
    private Coroutine _vignetteCoroutine;
    private Coroutine _shakeCoroutine;
    private Vector3  _camOriginalPos;

    private GameObject[] _colorOnlyObjects;
    private GameObject[] _monoOnlyObjects;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _cam = Camera.main;
        if (_cam == null) _cam = FindObjectOfType<Camera>();
        if (_cam != null)
        {
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _camOriginalPos = _cam.transform.localPosition;
        }

        CacheWorldObjects();
        ApplyWorldImmediate(IsMonoWorld);
    }

    private void Update()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shiftHeld && !IsMonoWorld) SetWorld(true);
        else if (!shiftHeld && IsMonoWorld) SetWorld(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// <summary>Force a world change from code (e.g. for timed level events).</summary>
    public void SetWorld(bool mono)
    {
        if (IsMonoWorld == mono) return;
        IsMonoWorld = mono;

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(mono));

        if (vignetteImage != null)
        {
            if (_vignetteCoroutine != null) StopCoroutine(_vignetteCoroutine);
            _vignetteCoroutine = StartCoroutine(VignetteFlash());
        }

        if (shakeCameraOnToggle && _cam != null)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(CameraShake());
        }

        ToggleWorldObjects(mono);
    }

    /// <summary>Re-scan scene for tagged objects (call after instantiating new level geometry).</summary>
    public void CacheWorldObjects()
    {
        _colorOnlyObjects = SafeFindByTag(colorOnlyTag);
        _monoOnlyObjects  = SafeFindByTag(monoOnlyTag);
    }

    /// <summary>FindGameObjectsWithTag but won't throw if the tag isn't defined yet.</summary>
    private GameObject[] SafeFindByTag(string tag)
    {
        try   { return GameObject.FindGameObjectsWithTag(tag); }
        catch { return new GameObject[0]; }
    }

    // ── Coroutines ────────────────────────────────────────────────────────────
    private IEnumerator TransitionRoutine(bool toMono)
    {
        Color from = _cam.backgroundColor;
        Color to   = toMono ? monoWorldBg : colorWorldBg;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
            _cam.backgroundColor = Color.Lerp(from, to, t);
            yield return null;
        }

        _cam.backgroundColor = to;
    }

    private IEnumerator VignetteFlash()
    {
        if (vignetteImage == null) yield break;
        vignetteImage.gameObject.SetActive(true);

        float half = vignetteDuration * 0.5f;
        float elapsed = 0f;

        // Fade in
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            SetVignetteAlpha(Mathf.Lerp(0f, vignettePeakAlpha, elapsed / half));
            yield return null;
        }
        // Fade out
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            SetVignetteAlpha(Mathf.Lerp(vignettePeakAlpha, 0f, elapsed / half));
            yield return null;
        }

        vignetteImage.gameObject.SetActive(false);
    }

    private IEnumerator CameraShake()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);
            _cam.transform.localPosition = _camOriginalPos + (Vector3)Random.insideUnitCircle * strength;
            yield return null;
        }
        _cam.transform.localPosition = _camOriginalPos;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void ApplyWorldImmediate(bool mono)
    {
        if (_cam != null)
            _cam.backgroundColor = mono ? monoWorldBg : colorWorldBg;
        ToggleWorldObjects(mono);
    }

    private void ToggleWorldObjects(bool mono)
    {
        foreach (var go in _colorOnlyObjects)
            if (go != null) go.SetActive(!mono);

        foreach (var go in _monoOnlyObjects)
            if (go != null) go.SetActive(mono);
    }

    private void SetVignetteAlpha(float a)
    {
        if (vignetteImage == null) return;
        Color c = vignetteImage.color;
        c.a = a;
        vignetteImage.color = c;
    }
}