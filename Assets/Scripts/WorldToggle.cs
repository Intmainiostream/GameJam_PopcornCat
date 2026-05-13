using System.Collections;
using System.Collections.Generic;
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

    // ── World Switch Sounds ───────────────────────────────────────────────────
    [Header("World Switch Sounds")]
    [Tooltip("Plays the moment Shift is pressed (entering Mono world).")]
    [SerializeField] private AudioClip shiftPressSound;
    [Tooltip("Plays the moment Shift is released (returning to Color world).")]
    [SerializeField] private AudioClip shiftReleaseSound;
    [Tooltip("Plays while Shift is held — loops until released.")]
    [SerializeField] private AudioClip shiftHoldSound;
    [Tooltip("Volume for all sounds.")]
    [SerializeField] [Range(0f, 1f)] private float switchSoundVolume = 1f;

    // ── Leaf Particles ────────────────────────────────────────────────────────
    [Header("Leaf Particles")]
    [Tooltip("Drag the LeafParticles GameObject here (under Particles/FX).")]
    [SerializeField] private ParticleSystem leafParticles;
    [Tooltip("How fast the leaf color transitions (seconds).")]
    [SerializeField] private float leafColorTransitionDuration = 0.25f;

    // ── Background Sprites (with adjustable dark color) ───────────────────────
    [Header("Background Sprites (Dark Gray in Mono)")]
    [Tooltip("Drag all background objects here (including their children)")]
    [SerializeField] private List<GameObject> backgroundSprites = new List<GameObject>();
    
    [Header("Dark Color Settings (Adjustable)")]
    [Tooltip("Choose the color when holding Shift (dark gray, dark blue, etc.)")]
    [SerializeField] private Color monoSpriteColor = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark gray - YOU CAN EDIT THIS
    
    [Tooltip("How fast the sprite color transitions (seconds)")]
    [SerializeField] private float spriteTransitionDuration = 0.25f;

    // ── Public ────────────────────────────────────────────────────────────────
    public static WorldToggle Instance { get; private set; }
    public bool IsMonoWorld { get; private set; } = false;

    // ── Private ───────────────────────────────────────────────────────────────
    private Camera               _cam;
    private Vector3              _camOriginPos;
    private CameraGlitchRenderer _glitchRenderer;

    private Coroutine   _bgRoutine;
    private Coroutine   _shakeRoutine;
    private Coroutine   _holdSoundRoutine;
    private Coroutine   _leafColorRoutine;
    private Coroutine   _spriteColorRoutine;
    private AudioSource _audioSource;

    private GameObject[] _colorOnlyObjects = new GameObject[0];
    private GameObject[] _monoOnlyObjects  = new GameObject[0];

    // Cached original leaf start color
    private Color _leafOriginalColor = Color.white;
    private Color _leafMonoColor     = Color.white;
    private bool  _leafColorCached   = false;
    private Color _leafCurrentColor;

    // For background sprites
    private Dictionary<SpriteRenderer, Color> originalSpriteColors = new Dictionary<SpriteRenderer, Color>();
    private List<SpriteRenderer> allSpriteRenderers = new List<SpriteRenderer>();
    private bool spriteColorsCached = false;

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

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop        = false;
        _audioSource.volume      = switchSoundVolume;

        // Cache colors
        CacheLeafColor();
        CacheSpriteColors();

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

        // ── Sound ─────────────────────────────────────────────────────────────
        if (_audioSource != null)
        {
            if (_holdSoundRoutine != null)
            {
                StopCoroutine(_holdSoundRoutine);
                _holdSoundRoutine = null;
            }
            _audioSource.loop = false;
            _audioSource.Stop();
            _audioSource.volume = switchSoundVolume;

            if (mono)
            {
                if (shiftPressSound != null)
                    _audioSource.PlayOneShot(shiftPressSound, switchSoundVolume);

                if (shiftHoldSound != null)
                    _holdSoundRoutine = StartCoroutine(PlayHoldSoundAfterPress());
            }
            else
            {
                if (shiftReleaseSound != null)
                    _audioSource.PlayOneShot(shiftReleaseSound, switchSoundVolume);
            }
        }

        // Background transition
        if (_bgRoutine != null) StopCoroutine(_bgRoutine);
        _bgRoutine = StartCoroutine(BackgroundRoutine(mono));

        // Camera shake
        if (enableShake && _cam != null)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine());
        }

        // Glitch effects
        if (_glitchRenderer != null)
            _glitchRenderer.TriggerEffects(mono);

        // Leaf particles transition
        if (_leafColorRoutine != null) StopCoroutine(_leafColorRoutine);
        _leafColorRoutine = StartCoroutine(LeafColorRoutine(mono));

        // Background sprites transition
        if (_spriteColorRoutine != null) StopCoroutine(_spriteColorRoutine);
        _spriteColorRoutine = StartCoroutine(SpriteColorRoutine(mono));

        ToggleWorldObjects(mono);
        ToggleDustAndFog(mono);
    }

    public void CacheWorldObjects()
    {
        _colorOnlyObjects = SafeFindByTag(colorOnlyTag);
        _monoOnlyObjects  = SafeFindByTag(monoOnlyTag);
    }

    // ── Coroutines ────────────────────────────────────────────────────────────
    private IEnumerator PlayHoldSoundAfterPress()
    {
        float wait = shiftPressSound != null ? shiftPressSound.length : 0f;
        yield return new WaitForSeconds(wait);

        if (IsMonoWorld && _audioSource != null && shiftHoldSound != null)
        {
            _audioSource.clip   = shiftHoldSound;
            _audioSource.loop   = true;
            _audioSource.volume = switchSoundVolume;
            _audioSource.Play();
        }
    }

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

    /// <summary>
    /// Transitions leaf particles to mono color and back
    /// </summary>
    private IEnumerator LeafColorRoutine(bool toMono)
    {
        if (leafParticles == null) yield break;

        Color fromColor = _leafCurrentColor;
        Color toColor   = toMono ? _leafMonoColor : _leafOriginalColor;

        float elapsed  = 0f;
        float duration = leafColorTransitionDuration > 0f ? leafColorTransitionDuration : 0.001f;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[leafParticles.main.maxParticles];

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Color blended = Color.Lerp(fromColor, toColor, t);
            _leafCurrentColor = blended;

            var mainModule = leafParticles.main;
            mainModule.startColor = new ParticleSystem.MinMaxGradient(blended);

            int count = leafParticles.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                Color pc = particles[i].startColor;
                particles[i].startColor = new Color(
                    Mathf.Lerp(fromColor.r, toColor.r, t),
                    Mathf.Lerp(fromColor.g, toColor.g, t),
                    Mathf.Lerp(fromColor.b, toColor.b, t),
                    pc.a);
            }
            leafParticles.SetParticles(particles, count);
            yield return null;
        }

        _leafCurrentColor = toColor;
        var finalModule = leafParticles.main;
        finalModule.startColor = new ParticleSystem.MinMaxGradient(toColor);

        int finalCount = leafParticles.GetParticles(particles);
        for (int i = 0; i < finalCount; i++)
        {
            Color pc = particles[i].startColor;
            particles[i].startColor = new Color(toColor.r, toColor.g, toColor.b, pc.a);
        }
        leafParticles.SetParticles(particles, finalCount);
    }

    /// <summary>
    /// Transitions all background sprites (including 2 child objects) to mono color and back
    /// </summary>
    private IEnumerator SpriteColorRoutine(bool toMono)
    {
        if (!spriteColorsCached || allSpriteRenderers.Count == 0) yield break;

        float elapsed = 0f;
        float duration = spriteTransitionDuration > 0f ? spriteTransitionDuration : 0.001f;

        // Store current colors and target colors
        Dictionary<SpriteRenderer, Color> currentColors = new Dictionary<SpriteRenderer, Color>();
        Dictionary<SpriteRenderer, Color> targetColors = new Dictionary<SpriteRenderer, Color>();

        foreach (SpriteRenderer sr in allSpriteRenderers)
        {
            if (sr == null) continue;
            currentColors[sr] = sr.color;
            
            if (toMono)
            {
                // Going to mono world - use the adjustable monoSpriteColor
                targetColors[sr] = new Color(
                    monoSpriteColor.r,
                    monoSpriteColor.g,
                    monoSpriteColor.b,
                    sr.color.a // preserve original alpha
                );
            }
            else
            {
                // Going back to color world - restore original color
                if (originalSpriteColors.ContainsKey(sr))
                {
                    targetColors[sr] = originalSpriteColors[sr];
                }
                else
                {
                    targetColors[sr] = sr.color;
                }
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            foreach (SpriteRenderer sr in allSpriteRenderers)
            {
                if (sr == null) continue;
                
                Color from = currentColors[sr];
                Color to = targetColors[sr];
                
                sr.color = new Color(
                    Mathf.Lerp(from.r, to.r, t),
                    Mathf.Lerp(from.g, to.g, t),
                    Mathf.Lerp(from.b, to.b, t),
                    Mathf.Lerp(from.a, to.a, t)
                );
            }
            
            yield return null;
        }

        // Final snap to target colors
        foreach (SpriteRenderer sr in allSpriteRenderers)
        {
            if (sr == null) continue;
            sr.color = targetColors[sr];
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void CacheLeafColor()
    {
        if (_leafColorCached || leafParticles == null) return;

        if (leafParticles.main.startColor.mode == ParticleSystemGradientMode.Color)
        {
            _leafOriginalColor = leafParticles.main.startColor.color;
        }
        else
        {
            _leafOriginalColor = Color.white;
        }
        
        _leafCurrentColor = _leafOriginalColor;
        _leafMonoColor = monoSpriteColor; // Use the same adjustable color
        _leafColorCached = true;
    }

    private void CacheSpriteColors()
    {
        if (spriteColorsCached) return;
        
        allSpriteRenderers.Clear();
        originalSpriteColors.Clear();
        
        foreach (GameObject bgObj in backgroundSprites)
        {
            if (bgObj == null) continue;
            
            // Get ALL SpriteRenderers in this object AND its children (including 2 child objects)
            SpriteRenderer[] renderers = bgObj.GetComponentsInChildren<SpriteRenderer>(true);
            
            foreach (SpriteRenderer sr in renderers)
            {
                if (!allSpriteRenderers.Contains(sr))
                {
                    allSpriteRenderers.Add(sr);
                    originalSpriteColors[sr] = sr.color;
                }
            }
        }
        
        spriteColorsCached = true;
        Debug.Log($"Cached {originalSpriteColors.Count} sprite colors from {backgroundSprites.Count} background objects (including children)");
    }

    private void ApplyWorldImmediate(bool mono)
    {
        if (_cam != null) _cam.backgroundColor = mono ? monoWorldBg : colorWorldBg;
        ToggleWorldObjects(mono);
        ToggleDustAndFog(mono);

        // Apply leaf color instantly
        if (leafParticles != null)
        {
            Color target = mono ? _leafMonoColor : _leafOriginalColor;
            _leafCurrentColor = target;

            var mainModule = leafParticles.main;
            mainModule.startColor = new ParticleSystem.MinMaxGradient(target);
            
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[leafParticles.main.maxParticles];
            int count = leafParticles.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                Color pc = particles[i].startColor;
                particles[i].startColor = new Color(target.r, target.g, target.b, pc.a);
            }
            leafParticles.SetParticles(particles, count);
        }

        // Apply sprite colors instantly
        if (allSpriteRenderers.Count > 0)
        {
            foreach (SpriteRenderer sr in allSpriteRenderers)
            {
                if (sr == null) continue;
                
                if (mono)
                {
                    sr.color = new Color(
                        monoSpriteColor.r,
                        monoSpriteColor.g,
                        monoSpriteColor.b,
                        sr.color.a
                    );
                }
                else
                {
                    if (originalSpriteColors.ContainsKey(sr))
                    {
                        sr.color = originalSpriteColors[sr];
                    }
                }
            }
        }
    }

    private void ToggleWorldObjects(bool mono)
    {
        foreach (var go in _colorOnlyObjects) if (go != null) go.SetActive(!mono);
        foreach (var go in _monoOnlyObjects)  if (go != null) go.SetActive(mono);
    }

    private void ToggleDustAndFog(bool mono)
    {
        if (dustMain != null)
        {
            if (mono) dustMain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else      dustMain.Play();
        }

        if (_glitchRenderer != null)
            _glitchRenderer.SetVignetteTarget(mono);
    }

    private GameObject[] SafeFindByTag(string tag)
    {
        try   { return GameObject.FindGameObjectsWithTag(tag); }
        catch { return new GameObject[0]; }
    }

    // Public method to refresh sprite cache (call if you add/remove sprites at runtime)
    public void RefreshSpriteCache()
    {
        spriteColorsCached = false;
        allSpriteRenderers.Clear();
        originalSpriteColors.Clear();
        CacheSpriteColors();
    }

    // ── Editor test ───────────────────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("Test → Mono")]
    private void TestMono()  => SetWorld(true);

    [ContextMenu("Test → Color")]
    private void TestColor() { IsMonoWorld = true; SetWorld(false); }
    
    [ContextMenu("Refresh Sprite Cache")]
    private void RefreshSpriteCacheEditor()
    {
        RefreshSpriteCache();
        Debug.Log("Sprite cache refreshed!");
    }
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
    private float _vignetteAlpha = 0f;
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

        _vignetteAlpha = Mathf.MoveTowards(_vignetteAlpha, _vignetteTarget,
                                            _vignetteFadeSpeed * dt);
    }

    // ── OnRenderImage ─────────────────────────────────────────────────────────
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

                GL.Color(new Color(1f, 0.1f, 0.1f, 0.18f * ct));
                GL.Vertex3(-offset,               sy, 0);
                GL.Vertex3(Screen.width - offset, sy, 0);
                GL.Vertex3(Screen.width - offset, ey, 0);
                GL.Vertex3(-offset,               ey, 0);

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

        // ── Mono vignette overlay ─────────────────────────────────────────────
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

                float ix0 = cx + Mathf.Cos(a0) * innerR;
                float iy0 = cy + Mathf.Sin(a0) * innerR;
                float ix1 = cx + Mathf.Cos(a1) * innerR;
                float iy1 = cy + Mathf.Sin(a1) * innerR;

                float ox0 = cx + Mathf.Cos(a0) * outerR;
                float oy0 = cy + Mathf.Sin(a0) * outerR;
                float ox1 = cx + Mathf.Cos(a1) * outerR;
                float oy1 = cy + Mathf.Sin(a1) * outerR;

                float finalAlpha = vc.a * _vignetteAlpha;
                Color transparent = new Color(vc.r, vc.g, vc.b, 0f);
                Color opaque      = new Color(vc.r, vc.g, vc.b, finalAlpha);

                GL.Color(transparent); GL.Vertex3(ix0, iy0, 0);
                GL.Color(opaque);      GL.Vertex3(ox0, oy0, 0);
                GL.Color(opaque);      GL.Vertex3(ox1, oy1, 0);

                GL.Color(transparent); GL.Vertex3(ix0, iy0, 0);
                GL.Color(opaque);      GL.Vertex3(ox1, oy1, 0);
                GL.Color(transparent); GL.Vertex3(ix1, iy1, 0);
            }
            GL.End();

            GL.Begin(GL.QUADS);
            GL.Color(new Color(vc.r, vc.g, vc.b, vc.a * _vignetteAlpha));
            GL.Vertex3(0,            0,  0); GL.Vertex3(Screen.width, 0,  0);
            GL.Vertex3(Screen.width, cy - outerR, 0); GL.Vertex3(0, cy - outerR, 0);

            GL.Vertex3(0,            cy + outerR, 0); GL.Vertex3(Screen.width, cy + outerR, 0);
            GL.Vertex3(Screen.width, Screen.height, 0); GL.Vertex3(0, Screen.height, 0);

            GL.Vertex3(0,            cy - outerR, 0); GL.Vertex3(cx - outerR, cy - outerR, 0);
            GL.Vertex3(cx - outerR,  cy + outerR, 0); GL.Vertex3(0,           cy + outerR, 0);

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