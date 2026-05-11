using UnityEngine;

public class WorldObject : MonoBehaviour
{
    public enum WorldType { MonoOnly, ColorOnly }

    [SerializeField] WorldType existsIn = WorldType.MonoOnly;
    [SerializeField] float ejectForce = 8f;

    SpriteRenderer sr;
    Collider2D col;
    Transform playerTransform;
    Rigidbody2D playerRb;
    Collider2D playerCol;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerRb        = playerObj.GetComponent<Rigidbody2D>();
            playerCol       = playerObj.GetComponent<Collider2D>();
        }
        Apply(false);
    }

    void OnEnable()  => PlayerMovement.OnWorldToggle += Apply;
    void OnDisable() => PlayerMovement.OnWorldToggle -= Apply;

    void Apply(bool isMonochrome)
    {
        bool visible    = existsIn == WorldType.MonoOnly ? isMonochrome : !isMonochrome;
        bool wasVisible = col != null && col.enabled;

        if (visible && !wasVisible)
            EjectPlayerIfOverlapping();
        else if (!visible && wasVisible)
            EjectPlayerIfOnTop();

        if (sr)  sr.enabled  = visible;
        if (col) col.enabled = visible;
    }

    void EjectPlayerIfOverlapping()
    {
        if (playerRb == null || playerCol == null) return;

        Bounds wall   = sr != null ? sr.bounds : new Bounds(transform.position, transform.localScale);
        Bounds player = playerCol.bounds;

        if (!wall.Intersects(player)) return;

        float dir = Mathf.Sign(playerTransform.position.x - transform.position.x);
        if (dir == 0f) dir = 1f;

        // teleport player to the edge so they don't clip inside
        float edgeX  = dir > 0 ? wall.max.x + player.extents.x + 0.05f
                                : wall.min.x - player.extents.x - 0.05f;
        playerRb.position = new Vector2(edgeX, playerRb.position.y);
        playerRb.velocity = new Vector2(dir * ejectForce, playerRb.velocity.y);
    }

    void EjectPlayerIfOnTop()
    {
        if (playerRb == null) return;

        Bounds wall = sr != null ? sr.bounds : new Bounds(transform.position, transform.localScale);
        Vector2 checkCenter = new Vector2(wall.center.x, wall.max.y + 0.1f);
        Vector2 checkSize   = new Vector2(wall.size.x * 0.9f, 0.3f);

        Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f);
        if (hit == null || hit.gameObject != playerRb.gameObject) return;

        float dir = Mathf.Sign(playerTransform.position.x - transform.position.x);
        if (dir == 0f) dir = 1f;
        playerRb.velocity = new Vector2(dir * ejectForce, playerRb.velocity.y);
    }
}
