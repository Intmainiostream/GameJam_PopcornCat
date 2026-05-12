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

    Rigidbody2D rb;
    float coyoteTimer;
    float jumpBufferTimer;
    bool isGrounded;
    bool isMonochrome;
    float defaultGravityScale;
    float visionTimer;
    float cooldownTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
    }

    void FixedUpdate()
    {
        float input = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(input * moveSpeed, rb.velocity.y);
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
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // Variable jump height — release Space early to cut jump short
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
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
