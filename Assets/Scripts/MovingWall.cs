using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingWall : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveDistance = 5f;
    [SerializeField] float moveSpeed    = 3f;

    Rigidbody2D rb;
    Vector2 startPos;
    bool movingRight = true;

    void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        startPos = rb.position;
    }

    void FixedUpdate()
    {
        float dir    = movingRight ? 1f : -1f;
        Vector2 next = rb.position + Vector2.right * dir * moveSpeed * Time.fixedDeltaTime;

        if (next.x >= startPos.x + moveDistance) movingRight = false;
        if (next.x <= startPos.x - moveDistance) movingRight = true;

        dir  = movingRight ? 1f : -1f;
        next = rb.position + Vector2.right * dir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(next);
    }
}
