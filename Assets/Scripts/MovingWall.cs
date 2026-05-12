using UnityEngine;

public class MovingWall : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveDistance = 5f;
    [SerializeField] float moveSpeed = 3f;

    Vector2 startPos;
    bool movingRight = true;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        MoveWall();
    }

    void MoveWall()
    {
        float dir = movingRight ? 1f : -1f;

        transform.Translate(Vector2.right * dir * moveSpeed * Time.deltaTime);

        if (transform.position.x >= startPos.x + moveDistance)
            movingRight = false;

        if (transform.position.x <= startPos.x - moveDistance)
            movingRight = true;
    }
}