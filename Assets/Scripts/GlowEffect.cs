using UnityEngine;

public class GlowEffect : MonoBehaviour
{
    [SerializeField] Color glowColor    = Color.yellow;
    [SerializeField] float minIntensity = 0.6f;
    [SerializeField] float maxIntensity = 1f;
    [SerializeField] float speed        = 2f;

    SpriteRenderer sr;

    void Start() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        float t         = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);

        if (sr != null)
            sr.color = new Color(glowColor.r * intensity, glowColor.g * intensity, glowColor.b * intensity, 1f);
    }
}
