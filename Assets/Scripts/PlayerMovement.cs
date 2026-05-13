using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float jumpForce = 16f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.15f;
    [SerializeField] LayerMask groundLayer;

    [Header("Jump Feel")]
    [SerializeField] float coyoteTime = 0.15f;
    [SerializeField] float jumpBufferTime = 0.15f;
    [SerializeField] float fallGravityScale = 2.5f;
    [SerializeField] float lowJumpGravityScale = 2f;

    [Header("The Visionary")]
    [SerializeField] float visionDuration = 3f;
    [SerializeField] float visionCooldown = 2f;

    public static event Action<bool> OnWorldToggle; // true = monochrome
    public static bool IsPlayerOverriding { get; private set; }
    public static void BroadcastWorldState(bool mono) => OnWorldToggle?.Invoke(mono);

    public float VisionFraction => visionDuration > 0f ? visionTimer / visionDuration : 0f;
    public float CooldownFraction => visionCooldown > 0f ? cooldownTimer / visionCooldown : 0f;
    public bool IsOnCooldown => cooldownTimer > 0f;
    public bool IsGroundedPublic => isGrounded;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    PlayerSounds sounds;
    float coyoteTimer;
    float jumpBufferTimer;
    bool isGrounded;
    bool isMonochrome;
    float defaultGravityScale;
    float visionTimer;
    float cooldownTimer;
    float jumpHeldTimer;
    bool jumpStarted;

    static readonly int SpeedHash      = Animator.StringToHash("Speed");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    static readonly int LightJumpHash  = Animator.StringToHash("LightJump");
    static readonly int BigJumpHash    = Animator.StringToHash("BigJump");

    void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        anim   = GetComponent<Animator>();
        sr     = GetComponent<SpriteRenderer>();
        sounds = GetComponent<PlayerSounds>();
        defaultGravityScale = rb.gravityScale;
        visionTimer = visionDuration;
    }

    void Update()
    {
        CheckGround();
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleJump();
        HandleWorldToggle();
        ApplyGravityFeel();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        float input = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(input * moveSpeed, rb.velocity.y);

        if (input != 0f) sr.flipX = input < 0f;
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetFloat(SpeedHash, Mathf.Abs(rb.velocity.x));
        anim.SetBool(IsGroundedHash, isGrounded);
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void HandleCoyoteTime()
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }

    void HandleJumpBuffer()
    {
        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump")) jumpHeldTimer = 0f;
        if (Input.GetButton("Jump"))     jumpHeldTimer += Time.deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.velocity     = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
            jumpStarted     = true;
            sounds?.PlayLongJump();
        }

        if (Input.GetButtonUp("Jump"))
        {
            if (rb.velocity.y > 0f)
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);

            if (jumpStarted && anim != null)
            {
                if (jumpHeldTimer < 0.2f) anim.SetTrigger(LightJumpHash);
                else anim.SetTrigger(BigJumpHash);
                jumpStarted = false;
            }
        }
    }

    void ApplyGravityFeel()
    {
        if (rb.velocity.y < 0f)
            rb.gravityScale = fallGravityScale;
        else if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
            rb.gravityScale = lowJumpGravityScale;
        else
            rb.gravityScale = defaultGravityScale;
    }

    void HandleWorldToggle()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
                visionTimer = visionDuration;

            if (isMonochrome)
            {
                isMonochrome = false;
                IsPlayerOverriding = false;
                ApplyAutoOrDefault();
            }
            return;
        }

        bool holding = Input.GetKey(KeyCode.LeftShift);

        if (holding && visionTimer > 0f)
        {
            IsPlayerOverriding = true;
            if (!isMonochrome)
            {
                isMonochrome = true;
                OnWorldToggle?.Invoke(true);
            }

            visionTimer -= Time.deltaTime;

            if (visionTimer <= 0f)
            {
                visionTimer = 0f;
                IsPlayerOverriding = false;
                cooldownTimer = visionCooldown;
                ApplyAutoOrDefault();
            }
        }
        else if (!holding && isMonochrome)
        {
            IsPlayerOverriding = false;
            cooldownTimer = visionCooldown;
            ApplyAutoOrDefault();
        }
    }

    void ApplyAutoOrDefault()
    {
        bool target = AutoWorldToggle.Instance != null
            ? AutoWorldToggle.Instance.CurrentDesiredState
            : false;

        isMonochrome = target;
        OnWorldToggle?.Invoke(target);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
