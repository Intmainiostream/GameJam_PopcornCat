using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSounds : MonoBehaviour
{
    [SerializeField] AudioClip lightJumpSfx;
    [SerializeField] AudioClip longJumpSfx;
    [SerializeField] AudioClip walkSfx;
    [SerializeField] AudioClip deathSfx;

    [SerializeField] float walkStepInterval = 0.35f;

    AudioSource audioSource;
    PlayerMovement movement;
    Rigidbody2D rb;

    float stepTimer;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        movement    = GetComponent<PlayerMovement>();
        rb          = GetComponent<Rigidbody2D>();

        audioSource.playOnAwake = false;
    }

    void OnEnable()  => PlayerDeath.OnPlayerDied += PlayDeath;
    void OnDisable() => PlayerDeath.OnPlayerDied -= PlayDeath;

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        bool moving    = Mathf.Abs(rb.velocity.x) > 0.1f;
        bool grounded  = !movement.IsOnCooldown || movement.VisionFraction > 0f;

        if (moving && movement.IsGroundedPublic)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                Play(walkSfx);
                stepTimer = walkStepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    public void PlayLightJump() => Play(lightJumpSfx);
    public void PlayLongJump()  => Play(longJumpSfx);
    void PlayDeath()            => Play(deathSfx);

    void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }
}
