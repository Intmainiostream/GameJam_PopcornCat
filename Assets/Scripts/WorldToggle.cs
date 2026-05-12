using System.Collections;
using UnityEngine;

/// <summary>
/// WorldToggle — Chromablind
/// Attach to GameManager (or any GameObject).
/// Glitch/aberration/flash are rendered via CameraGlitchRenderer,
/// which is automatically added to the Main Camera at runtime.
/// Hold Shift → Mono world. Release → Color world.
/// </summary>
public class WorldToggle : MonoBehaviour
{
    // ── Background ────────────────────────────────────────────────────────────
    [Header("Background Colors")]
    [SerializeField] private Color colorWorldBg      = new Color(0.102f, 0.086f, 0.188f);
    [SerializeField] private Color monoWorldBg       = new Color(0.102f, 0.102f, 0.102f);
    [SerializeField] private float transitionDuration = 0.25f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ── Camera Shake ──────────────────────────────────────────────────────────
    [Header("Camera Shake")]
    [SerializeField] private bool  enableShake    = true;
    [SerializeField] private float shakeDuration  = 0.22f;
    [SerializeField] private float shakeMagnitude = 0.14f;
    [SerializeField] private float shakeRoughness = 28f;
    [SerializeField] private float rotationAmount = 0.5f;

    // ── Glitch Lines ──────────────────────────────────────────────────────────
    [Header("Glitch Lines")]
    [SerializeField] private int   glitchLineCount    = 18;
    [SerializeField] private float glitchLineDuration = 0.35f;
    [SerializeField] private Color glitchColorWorld   = new Color(0.63f, 0.55f, 0.95f, 0.55f);
    [SerializeField] private Color glitchMonoWorld    = new Color(0.85f, 0.85f, 0.85f, 0.45f);

    // ── Chromatic Aberration ──────────────────────────────────────────────────
    [Header("Chromatic Aberration")]
    [SerializeField] private float chromaticDuration = 0.28f;
    [SerializeField] private float chromaticStrength = 0.025f;

    // ── Screen Flash ──────────────────────────────────────────────────────────
    [Header("Screen Flash")]
    [SerializeField] private float flashDuration  = 0.12f;
    [SerializeField] private float flashPeakAlpha = 0.28f;

    // ── World Object Tags ─────────────────────────────────────────────────────
    [Header("World Object Tags")]
    [SerializeField] private string colorOnlyTag = "ColorOnly";
    [SerializeField] private string monoOnlyTag  = "MonoOnly";

    // ── Dust / Fog ────────────────────────────────────────────────────────────
    [Header("Dust & Mono Fog")]
    [Tooltip("Drag the DustMain GameObject here (under Particles/FX).")]
    [SerializeField] private ParticleSystem dustMain;

    [Tooltip("Color of the vignette shown in Mono world (usually dark).")]
    [SerializeField] private Color vignetteColor     = new Color(0f, 0f, 0f, 0.82f);
    [Tooltip("How fast the vignette fades in/out on world switch (seconds).")]
    [SerializeField] private float vignetteFadeSpeed = 3.5f;
    [Tooltip("Inner radius — fraction of half-screen diagonal where vignette starts (0=center,1=edge).")]
    [SerializeField] private float vignetteInner     = 0.45f;
    [Tooltip("Outer radius — fraction where vignette reaches full opacity.")]
    [SerializeField] private float vignetteOuter     = 1.05f;
    [Tooltip("Number of radial steps used to draw the vignette (more = smoother).")]
    [SerializeField] private int   vignetteSteps     = 32;

    // ── Public ────────────────────────────────────────────────────────────────
    public static WorldToggle Instance { get; private set; }
    public bool IsMonoWorld { get; private set; } = false;

    // ── Private ───────────────────────────────────────────────────────────────
    private Camera               _cam;
    private Vector3              _camOriginPos;
    private CameraGlitchRenderer _glitchRenderer;

    private Coroutine _bgRoutine;
    private Coroutine _shakeRoutine;

    private GameObject[] _colorOnlyObjects = new GameObject[0];
    private GameObject[] _monoOnlyObjects  = new GameObject[0];

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _cam = Camera.main ?? FindObjectOfType<Camera>();

        if (_cam != null)
        {
            _cam.clearFlags  = CameraClearFlags.SolidColor;
            _camOriginPos    = _cam.transform.localPosition;

            // Attach the renderer component directly to the Camera so
            // OnRenderImage is called correctly — this is the key fix.
            _glitchRenderer = _cam.gameObject.GetComponent<CameraGlitchRenderer>();
            if (_glitchRenderer == null)
                _glitchRenderer = _cam.gameObject.AddComponent<CameraGlitchRenderer>();

            _glitchRenderer.Init(glitchLineCount, glitchLineDuration,
                                 glitchColorWorld, glitchMonoWorld,
                                 chromaticDuration, chromaticStrength,
                                 flashDuration, flashPeakAlpha,
                                 vignetteColor, vignetteFadeSpeed,
                                 vignetteInner, vignetteOuter, vignetteSteps);
        }

        CacheWorldObjects();
        ApplyWorldImmediate(false);
    }

    private void OnEnable()  => PlayerMovement.OnWorldToggle += SetWorld;
    private void OnDisable() => PlayerMovement.OnWorldToggle -= SetWorld;

    // ── Public API ────────────────────────────────────────────────────────────
    public void SetWorld(bool mono)
    {
        if (IsMonoWorld == mono) return;
        IsMonoWorld = mono;

        // Background transition
        if (_bgRoutine != null) StopCoroutine(_bgRoutine);
        _bgRoutine = StartCoroutine(BackgroundRoutine(mono));

        // Camera shake
        if (enableShake && _cam != null)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine());
        }

        // Trigger all glitch effects on the camera-side renderer
        if (_glitchRenderer != null)
            _glitchRenderer.TriggerEffects(mono);

        ToggleWorldObjects(mono);
        ToggleDustAndFog(mono);
    }

    public void CacheWorldObjects()
    {
        _colorOnlyObjects = SafeFindByTag(colorOnlyTag);
        _monoOnlyObjects  = SafeFindByTag(monoOnlyTag);
    }

    // ── Coroutines ────────────────────────────────────────────────────────────
    private IEnumerator BackgroundRoutine(bool toMono)
    {
        Color from    = _cam.backgroundColor;
        Color to      = toMono ? monoWorldBg : colorWorldBg;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed             += Time.deltaTime;
            float t              = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
            _cam.backgroundColor = Color.Lerp(from, to, t);
            yield return null;
        }

        _cam.backgroundColor = to;
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        float seed    = Random.value * 100f;

        while (elapsed < shakeDuration)
        {
            elapsed     += Time.deltaTime;
            float dampen = 1f - Mathf.Pow(elapsed / shakeDuration, 2f);

            float nx = (Mathf.PerlinNoise(seed,        elapsed * shakeRoughness) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(seed + 100f, elapsed * shakeRoughness) - 0.5f) * 2f;
            float nr = (Mathf.PerlinNoise(seed + 200f, elapsed * shakeRoughness) - 0.5f) * 2f;

            _cam.transform.localPosition = _camOriginPos + new Vector3(
                nx * shakeMagnitude * dampen,
                ny * shakeMagnitude * dampen, 0f);

            _cam.transform.localRotation = Quaternion.Euler(0f, 0f, nr * rotationAmount * dampen);
            yield return null;
        }

        _cam.transform.localPosition = _camOriginPos;
        _cam.transform.localRotation = Quaternion.identity;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void ApplyWorldImmediate(bool mono)
    {
        if (_cam != null) _cam.backgroundColor = mono ? monoWorldBg : colorWorldBg;
        ToggleWorldObjects(mono);
        ToggleDustAndFog(mono);
    }

    private void ToggleWorldObjects(bool mono)
    {
        foreach (var go in _colorOnlyObjects) if (go != null) go.SetActive(!mono);
        foreach (var go in _monoOnlyObjects)  if (go != null) go.SetActive(mono);
    }

    private void ToggleDustAndFog(bool mono)
    {
        // Hide DustMain particles in Mono world; restore in Color world
        if (dustMain != null)
        {
            if (mono) dustMain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else      dustMain.Play();
        }

        // Tell the glitch renderer to fade vignette in/out
        if (_glitchRenderer != null)
            _glitchRenderer.SetVignetteTarget(mono);
    }

    private GameObject[] SafeFindByTag(string tag)
    {
        try   { return GameObject.FindGameObjectsWithTag(tag); }
        catch { return new GameObject[0]; }
    }

    // ── Editor test ───────────────────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("Test → Mono")]
    private void TestMono()  => SetWorld(true);

    [ContextMenu("Test → Color")]
    private void TestColor() { IsMonoWorld = true; SetWorld(false); }
#endif
}


/// <summary>
/// CameraGlitchRenderer
/// Automatically added to the Main Camera by WorldToggle at runtime.
/// Handles OnRenderImage so glitch effects actually draw on screen.
/// Do NOT add this manually in the Inspector.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraGlitchRenderer : MonoBehaviour
{
    // ── Config (set by WorldToggle.Init) ──────────────────────────────────────
    private int   _lineCount;
    private float _lineDuration;
    private Color _glitchColor;
    private Color _glitchMono;
    private float _chromaDuration;
    private float _chromaStrength;
    private float _flashDuration;
    private float _flashPeakAlpha;

    // ── Vignette config ───────────────────────────────────────────────────────
    private Color _vignetteColor;
    private float _vignetteFadeSpeed;
    private float _vignetteInner;
    private float _vignetteOuter;
    private int   _vignetteSteps;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private bool  _toMono;

    private bool  _glitching;
    private float _glitchTimer;

    private bool  _chromaActive;
    private float _chromaTimer;

    private float _flashAlpha;
    private float _vignetteAlpha = 0f;   // 0=off 1=fully on
    private float _vignetteTarget = 0f;

    private struct GlitchLine
    {
        public float y, x, w, thick, speed;
    }
    private GlitchLine[] _lines;

    private Material _mat;

    // ── Initialise ────────────────────────────────────────────────────────────
    public void Init(int lineCount, float lineDuration,
                     Color glitchColor, Color glitchMono,
                     float chromaDuration, float chromaStrength,
                     float flashDuration, float flashPeakAlpha,
                     Color vignetteColor, float vignetteFadeSpeed,
                     float vignetteInner, float vignetteOuter, int vignetteSteps)
    {
        _lineCount          = lineCount;
        _lineDuration       = lineDuration;
        _glitchColor        = glitchColor;
        _glitchMono         = glitchMono;
        _chromaDuration     = chromaDuration;
        _chromaStrength     = chromaStrength;
        _flashDuration      = flashDuration;
        _flashPeakAlpha     = flashPeakAlpha;
        _vignetteColor      = vignetteColor;
        _vignetteFadeSpeed  = vignetteFadeSpeed;
        _vignetteInner      = vignetteInner;
        _vignetteOuter      = vignetteOuter;
        _vignetteSteps      = vignetteSteps;

        // Unlit blended material for GL
        _mat = new Material(Shader.Find("Hidden/Internal-Colored"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _mat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
        _mat.SetInt("_ZWrite",   0);
    }

    public void SetVignetteTarget(bool mono) => _vignetteTarget = mono ? 1f : 0f;

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }

    // ── Public trigger ────────────────────────────────────────────────────────
    public void TriggerEffects(bool toMono)
    {
        _toMono = toMono;

        GenerateGlitchLines();
        _glitching   = true;
        _glitchTimer = _lineDuration;

        _chromaActive = true;
        _chromaTimer  = _chromaDuration;

        _flashAlpha = _flashPeakAlpha;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        float dt = Time.deltaTime;

        if (_glitching)
        {
            _glitchTimer -= dt;
            if (_glitchTimer <= 0f) _glitching = false;
        }

        if (_chromaActive)
        {
            _chromaTimer -= dt;
            if (_chromaTimer <= 0f) _chromaActive = false;
        }

        if (_flashAlpha > 0f)
            _flashAlpha = Mathf.Max(0f, _flashAlpha - dt * (_flashPeakAlpha / (_flashDuration * 0.5f)));

        // Smooth vignette fade in/out
        _vignetteAlpha = Mathf.MoveTowards(_vignetteAlpha, _vignetteTarget,
                                            _vignetteFadeSpeed * dt);
    }

    // ── OnRenderImage — must be on the Camera GameObject ─────────────────────
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!_glitching && !_chromaActive && _flashAlpha <= 0f && _vignetteAlpha <= 0f)
        {
            Graphics.Blit(src, dest);
            return;
        }

        Graphics.Blit(src, dest);

        RenderTexture.active = dest;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
        _mat.SetPass(0);

        // ── Scanline glitch strips ────────────────────────────────────────────
        if (_glitching && _lines != null)
        {
            float t         = Mathf.Clamp01(_glitchTimer / _lineDuration);
            Color lineColor = _toMono ? _glitchMono : _glitchColor;

            GL.Begin(GL.QUADS);
            foreach (var l in _lines)
            {
                float alpha = lineColor.a * t * (0.4f + 0.6f * Mathf.Sin(Time.time * 40f + l.y * 10f));
                GL.Color(new Color(lineColor.r, lineColor.g, lineColor.b, alpha));

                float px = (l.x + Mathf.Sin(Time.time * l.speed) * 0.05f) * Screen.width;
                float py = l.y * Screen.height;
                float pw = l.w * Screen.width;
                float ph = l.thick;

                GL.Vertex3(px,      py,      0);
                GL.Vertex3(px + pw, py,      0);
                GL.Vertex3(px + pw, py + ph, 0);
                GL.Vertex3(px,      py + ph, 0);
            }
            GL.End();
        }

        // ── Chromatic aberration strips ───────────────────────────────────────
        if (_chromaActive)
        {
            float ct     = Mathf.Clamp01(_chromaTimer / _chromaDuration);
            float offset = _chromaStrength * Screen.width * ct;
            float stripH = Screen.height / 12f;

            GL.Begin(GL.QUADS);
            for (int i = 0; i < 6; i++)
            {
                float sy = (i / 6f) * Screen.height;
                float ey = sy + stripH * 0.6f;

                // Red — shifted left
                GL.Color(new Color(1f, 0.1f, 0.1f, 0.18f * ct));
                GL.Vertex3(-offset,               sy, 0);
                GL.Vertex3(Screen.width - offset, sy, 0);
                GL.Vertex3(Screen.width - offset, ey, 0);
                GL.Vertex3(-offset,               ey, 0);

                // Cyan — shifted right
                GL.Color(new Color(0.1f, 0.9f, 1f, 0.18f * ct));
                GL.Vertex3(offset,                sy, 0);
                GL.Vertex3(Screen.width + offset, sy, 0);
                GL.Vertex3(Screen.width + offset, ey, 0);
                GL.Vertex3(offset,                ey, 0);
            }
            GL.End();
        }

        // ── Full-screen flash ─────────────────────────────────────────────────
        if (_flashAlpha > 0f)
        {
            Color fc = _toMono
                ? new Color(1f,    1f,    1f,    _flashAlpha)
                : new Color(0.63f, 0.55f, 0.95f, _flashAlpha);

            GL.Begin(GL.QUADS);
            GL.Color(fc);
            GL.Vertex3(0,            0,             0);
            GL.Vertex3(Screen.width, 0,             0);
            GL.Vertex3(Screen.width, Screen.height, 0);
            GL.Vertex3(0,            Screen.height, 0);
            GL.End();
        }

        // ── Mono vignette overlay ────────────────────────────────────────────
        if (_vignetteAlpha > 0f)
        {
            float cx     = Screen.width  * 0.5f;
            float cy     = Screen.height * 0.5f;
            float halfDiag = Mathf.Sqrt(cx * cx + cy * cy);
            float innerR = _vignetteInner * halfDiag;
            float outerR = _vignetteOuter * halfDiag;
            int   steps  = Mathf.Max(8, _vignetteSteps);
            float step   = 360f / steps;

            Color vc = _vignetteColor;

            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < steps; i++)
            {
                float a0 = i       * step * Mathf.Deg2Rad;
                float a1 = (i + 1) * step * Mathf.Deg2Rad;

                // Inner point — transparent
                float ix0 = cx + Mathf.Cos(a0) * innerR;
                float iy0 = cy + Mathf.Sin(a0) * innerR;
                float ix1 = cx + Mathf.Cos(a1) * innerR;
                float iy1 = cy + Mathf.Sin(a1) * innerR;

                // Outer point — full vignette alpha
                float ox0 = cx + Mathf.Cos(a0) * outerR;
                float oy0 = cy + Mathf.Sin(a0) * outerR;
                float ox1 = cx + Mathf.Cos(a1) * outerR;
                float oy1 = cy + Mathf.Sin(a1) * outerR;

                float finalAlpha = vc.a * _vignetteAlpha;
                Color transparent = new Color(vc.r, vc.g, vc.b, 0f);
                Color opaque      = new Color(vc.r, vc.g, vc.b, finalAlpha);

                // Triangle 1 — inner edge to outer edge
                GL.Color(transparent); GL.Vertex3(ix0, iy0, 0);
                GL.Color(opaque);      GL.Vertex3(ox0, oy0, 0);
                GL.Color(opaque);      GL.Vertex3(ox1, oy1, 0);

                // Triangle 2 — close the quad
                GL.Color(transparent); GL.Vertex3(ix0, iy0, 0);
                GL.Color(opaque);      GL.Vertex3(ox1, oy1, 0);
                GL.Color(transparent); GL.Vertex3(ix1, iy1, 0);
            }
            GL.End();

            // Fill corners beyond outerR (screen edges) with solid color
            GL.Begin(GL.QUADS);
            GL.Color(new Color(vc.r, vc.g, vc.b, vc.a * _vignetteAlpha));
            // Top strip
            GL.Vertex3(0,            0,  0); GL.Vertex3(Screen.width, 0,  0);
            GL.Vertex3(Screen.width, cy - outerR, 0); GL.Vertex3(0, cy - outerR, 0);
            // Bottom strip
            GL.Vertex3(0,            cy + outerR, 0); GL.Vertex3(Screen.width, cy + outerR, 0);
            GL.Vertex3(Screen.width, Screen.height, 0); GL.Vertex3(0, Screen.height, 0);
            // Left strip
            GL.Vertex3(0,            cy - outerR, 0); GL.Vertex3(cx - outerR, cy - outerR, 0);
            GL.Vertex3(cx - outerR,  cy + outerR, 0); GL.Vertex3(0,           cy + outerR, 0);
            // Right strip
            GL.Vertex3(cx + outerR,  cy - outerR, 0); GL.Vertex3(Screen.width, cy - outerR, 0);
            GL.Vertex3(Screen.width, cy + outerR, 0); GL.Vertex3(cx + outerR,  cy + outerR, 0);
            GL.End();
        }

        GL.PopMatrix();
        RenderTexture.active = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void GenerateGlitchLines()
    {
        _lines = new GlitchLine[_lineCount];
        for (int i = 0; i < _lineCount; i++)
        {
            _lines[i] = new GlitchLine
            {
                y     = Random.value,
                x     = Random.value * 0.3f,
                w     = 0.2f + Random.value * 0.7f,
                thick = 1f + Random.value * 6f,
                speed = 5f + Random.value * 20f,
            };
        }
    }
}