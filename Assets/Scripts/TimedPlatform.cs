using UnityEngine;

public class TimedPlatform : MonoBehaviour
{
    public enum WorldType { MonoOnly, ColorOnly }
    public enum Difficulty { Default, Extreme }

    enum Phase { Active, Blinking, Respawning }

    [SerializeField] WorldType existsIn      = WorldType.MonoOnly;
    [SerializeField] Difficulty difficulty   = Difficulty.Default;
    [SerializeField] float activeDuration    = 3f;
    [SerializeField] float blinkDuration     = 1f;
    [SerializeField] float blinkRate         = 0.1f;
    [SerializeField] float respawnDelay      = 3f;

    [HideInInspector] [SerializeField] Difficulty lastDifficulty = Difficulty.Default;

    void OnValidate()
    {
        if (difficulty == lastDifficulty) return;
        lastDifficulty = difficulty;

        if (difficulty == Difficulty.Default)
        {
            activeDuration = 3f;
            blinkDuration  = 1f;
            blinkRate      = 0.1f;
            respawnDelay   = 3f;
        }
        else
        {
            activeDuration = 1.5f;
            blinkDuration  = 0.5f;
            blinkRate      = 0.05f;
            respawnDelay   = 5f;
        }
    }

    SpriteRenderer sr;
    Collider2D col;

    Phase phase = Phase.Active;
    float timer;
    float blinkAccum;
    bool isWorldActive;
    bool hasStarted;

    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        SetState(false);
    }

    void OnEnable()  => PlayerMovement.OnWorldToggle += OnWorldToggle;
    void OnDisable() => PlayerMovement.OnWorldToggle -= OnWorldToggle;

    void OnWorldToggle(bool isMonochrome)
    {
        bool shouldBeActive = existsIn == WorldType.MonoOnly ? isMonochrome : !isMonochrome;
        isWorldActive = shouldBeActive;

        if (shouldBeActive && !hasStarted)
        {
            hasStarted = true;
            phase = Phase.Active;
            timer = activeDuration;
            SetState(true);
        }
        else if (shouldBeActive)
        {
            // resume — restore visual based on current phase
            bool visible = phase == Phase.Active || phase == Phase.Blinking;
            SetState(visible);
        }
        else
        {
            // pause — hide but keep timer exactly where it is
            SetState(false);
        }
    }

    void Update()
    {
        if (!isWorldActive) return;

        timer -= Time.deltaTime;

        switch (phase)
        {
            case Phase.Active:
                if (timer <= 0f)
                {
                    phase = Phase.Blinking;
                    timer = blinkDuration;
                    blinkAccum = 0f;
                }
                break;

            case Phase.Blinking:
                blinkAccum += Time.deltaTime;
                if (blinkAccum >= blinkRate)
                {
                    blinkAccum = 0f;
                    sr.enabled = !sr.enabled;
                }
                if (timer <= 0f)
                {
                    phase = Phase.Respawning;
                    timer = respawnDelay;
                    SetState(false);
                }
                break;

            case Phase.Respawning:
                if (timer <= 0f)
                {
                    phase = Phase.Active;
                    timer = activeDuration;
                    SetState(true);
                }
                break;
        }
    }

    void SetState(bool visible)
    {
        sr.enabled  = visible;
        col.enabled = visible;
    }
}
