using UnityEngine;

public class WorldSprite : MonoBehaviour
{
    [SerializeField] Sprite colorSprite;
    [SerializeField] Sprite monoSprite;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Apply(false);
    }

    void OnEnable()  => PlayerMovement.OnWorldToggle += Apply;
    void OnDisable() => PlayerMovement.OnWorldToggle -= Apply;

    void Apply(bool isMonochrome) => sr.sprite = isMonochrome ? monoSprite : colorSprite;
}
