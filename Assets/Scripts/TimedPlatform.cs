using System.Collections;
using UnityEngine;

public class TimedPlatform : MonoBehaviour
{
    public enum WorldType { MonoOnly, ColorOnly }

    [SerializeField] WorldType existsIn     = WorldType.MonoOnly;
    [SerializeField] float activeDuration   = 3f;
    [SerializeField] float blinkDuration    = 1f;
    [SerializeField] float blinkRate        = 0.1f;
    [SerializeField] float respawnDelay     = 3f;

    SpriteRenderer sr;
    Collider2D col;
    Coroutine cycle;

    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        SetVisible(false);
        col.enabled = false;
    }

    void OnEnable()  => PlayerMovement.OnWorldToggle += OnWorldToggle;
    void OnDisable() => PlayerMovement.OnWorldToggle -= OnWorldToggle;

    void OnWorldToggle(bool isMonochrome)
    {
        bool shouldBeActive = existsIn == WorldType.MonoOnly ? isMonochrome : !isMonochrome;

        if (shouldBeActive)
        {
            if (cycle != null) StopCoroutine(cycle);
            cycle = StartCoroutine(Cycle());
        }
        else
        {
            if (cycle != null) StopCoroutine(cycle);
            cycle = null;
            SetVisible(false);
            col.enabled = false;
        }
    }

    IEnumerator Cycle()
    {
        while (true)
        {
            SetVisible(true);
            col.enabled = true;
            yield return new WaitForSeconds(activeDuration);

            float t = 0f;
            while (t < blinkDuration)
            {
                sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(blinkRate);
                t += blinkRate;
            }

            SetVisible(false);
            col.enabled = false;
            yield return new WaitForSeconds(respawnDelay);
        }
    }

    void SetVisible(bool visible) => sr.enabled = visible;
}
