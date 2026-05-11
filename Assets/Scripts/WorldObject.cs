using UnityEngine;

public class WorldObject : MonoBehaviour
{
    public enum WorldType { MonoOnly, ColorOnly }

    [SerializeField] WorldType existsIn = WorldType.MonoOnly;

    SpriteRenderer sr;
    Collider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        Apply(false);
    }

    void OnEnable()  => PlayerMovement.OnWorldToggle += Apply;
    void OnDisable() => PlayerMovement.OnWorldToggle -= Apply;

    void Apply(bool isMonochrome)
    {
        bool visible = existsIn == WorldType.MonoOnly ? isMonochrome : !isMonochrome;
        if (sr)  sr.enabled  = visible;
        if (col) col.enabled = visible;
    }
}
