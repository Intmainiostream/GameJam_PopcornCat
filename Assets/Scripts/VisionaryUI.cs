using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VisionaryUI : MonoBehaviour
{
    [SerializeField] Image eyeFill;
    [SerializeField] PlayerMovement player;

    [Header("Warning Shake")]
    [SerializeField] float shakeMagnitude = 8f;
    [SerializeField] float shakeDuration  = 0.5f;

    RectTransform eyeRect;
    Vector2 eyeOrigin;

    void Awake()
    {
        eyeRect   = eyeFill != null ? eyeFill.GetComponentInParent<RectTransform>() : null;
        if (eyeRect != null) eyeOrigin = eyeRect.anchoredPosition;
    }

    void OnEnable()  => AutoWorldToggle.OnWarning += TriggerShake;
    void OnDisable() => AutoWorldToggle.OnWarning -= TriggerShake;

    void Update()
    {
        if (player == null || eyeFill == null) return;

        eyeFill.fillAmount = player.IsOnCooldown
            ? 1f - player.CooldownFraction
            : player.VisionFraction;
    }

    void TriggerShake() => StartCoroutine(Shake());

    IEnumerator Shake()
    {
        if (eyeRect == null) yield break;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);
            eyeRect.anchoredPosition = eyeOrigin + Random.insideUnitCircle * strength;
            yield return null;
        }
        eyeRect.anchoredPosition = eyeOrigin;
    }
}
