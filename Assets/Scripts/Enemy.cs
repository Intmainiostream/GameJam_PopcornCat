using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] float patrolRadius = 3f;
    [SerializeField] float patrolSpeed  = 2f;
    [SerializeField] float chaseSpeed   = 4f;

    [Header("Color World")]
    [SerializeField] RuntimeAnimatorController colorAnim;

    [Header("Mono World")]
    [SerializeField] RuntimeAnimatorController monoAnim;

    SpriteRenderer sr;
    Animator anim;
    Transform player;
    Vector2 origin;
    bool movingRight = true;
    bool isMonochrome;

    void Awake()
    {
        sr   = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        origin = transform.position;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        ApplyWorld(false);
    }

    void OnEnable()  => PlayerMovement.OnWorldToggle += ApplyWorld;
    void OnDisable() => PlayerMovement.OnWorldToggle -= ApplyWorld;

    void ApplyWorld(bool mono)
    {
        isMonochrome = mono;
        if (anim != null)
            anim.runtimeAnimatorController = mono ? monoAnim : colorAnim;
    }

    void Update()
    {
        if (isMonochrome && player != null && Vector2.Distance(transform.position, player.position) <= patrolRadius)
            Chase();
        else
            Patrol();
    }

    void Patrol()
    {
        float dir = movingRight ? 1f : -1f;
        transform.Translate(Vector2.right * dir * patrolSpeed * Time.deltaTime);
        sr.flipX = !movingRight;

        if (transform.position.x >= origin.x + patrolRadius) movingRight = false;
        if (transform.position.x <= origin.x - patrolRadius) movingRight = true;
    }

    void Chase()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.Translate(dir * chaseSpeed * Time.deltaTime);
        sr.flipX = dir.x < 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isMonochrome) return;
        PlayerDeath death = other.GetComponent<PlayerDeath>();
        if (death != null) death.Die();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isMonochrome ? Color.red : Color.cyan;
        Vector3 center = Application.isPlaying ? (Vector3)origin : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}
