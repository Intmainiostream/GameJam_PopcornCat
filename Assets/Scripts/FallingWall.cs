using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingWall : MonoBehaviour
{
    public static event Action OnFallShakeStarted;

    [Header("Fall Settings")]
    [SerializeField] private float fallDelay      = 0.5f;
    [SerializeField] private float shakeAmplitude = 0.08f;
    [SerializeField] private float shakeFrequency = 30f;
    [SerializeField] private bool  fallOnce       = true;

    [Header("Landing")]
    [SerializeField] private float destroyDelay   = 0f;
    [SerializeField] private ParticleSystem dustParticles;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxShake;
    [SerializeField] private AudioClip sfxLand;
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("Detection")]
    [SerializeField] private string triggerTag        = "Player";
    [SerializeField] private float  topCheckThickness = 0.15f;
    [SerializeField] private float  topCheckWidth     = 0.9f;

    private Rigidbody2D  _rb;
    private Collider2D   _col;
    private Vector2      _originPos;
    private bool         _triggered = false;
    private bool         _falling   = false;
    private bool         _landed    = false;
    private Coroutine    _routine;
    private AudioSource  _audio;

    void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        _rb.bodyType      = RigidbodyType2D.Kinematic;
        _rb.gravityScale  = 2f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.constraints   = RigidbodyConstraints2D.FreezeRotation;

        _originPos = _rb.position;

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake  = false;
        _audio.spatialBlend = 0f;
        _audio.volume       = sfxVolume;
    }

    void Update()
    {
        if (!_triggered && IsPlayerOnTop())
            Trigger();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_falling && !_landed && col.gameObject.CompareTag("Ground"))
            OnLanded();
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (_falling && !_landed && col.gameObject.CompareTag("Ground"))
            OnLanded();
    }

    bool IsPlayerOnTop()
    {
        Bounds b = _col.bounds;
        Vector2 center = new Vector2(b.center.x, b.max.y + topCheckThickness * 0.5f);
        Vector2 size   = new Vector2(b.size.x * topCheckWidth, topCheckThickness);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (var hit in hits)
            if (hit != null && hit.CompareTag(triggerTag)) return true;
        return false;
    }

    public void Trigger()
    {
        if (_triggered && fallOnce) return;
        _triggered = true;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FallSequence());
    }

    public void ResetWall()
    {
        StopAllCoroutines();
        _triggered = _falling = _landed = false;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.velocity = Vector2.zero;
        _rb.position = _originPos;
        transform.position = new Vector3(_originPos.x, _originPos.y, transform.position.z);
        if (dustParticles != null) dustParticles.Stop();
    }

    IEnumerator FallSequence()
    {
        OnFallShakeStarted?.Invoke();
        PlaySfx(sfxShake, loop: true);

        float elapsed = 0f;
        while (elapsed < fallDelay)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(0.3f, 1f, elapsed / fallDelay);
            float offsetX   = Mathf.Sin(elapsed * shakeFrequency) * shakeAmplitude * intensity;

            // MovePosition — the correct way to move a Kinematic Rigidbody2D
            _rb.MovePosition(new Vector2(_originPos.x + offsetX, _originPos.y));

            yield return new WaitForFixedUpdate();
        }

        _rb.MovePosition(_originPos);
        StopSfx();

        _falling     = true;
        _rb.bodyType = RigidbodyType2D.Dynamic;
    }

    void OnLanded()
    {
        if (_landed) return;
        _landed      = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.velocity = Vector2.zero;

        PlaySfx(sfxLand);

        if (dustParticles != null)
        {
            dustParticles.transform.position = transform.position;
            dustParticles.Play();
        }

        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    void PlaySfx(AudioClip clip, bool loop = false)
    {
        if (clip == null || _audio == null) return;
        _audio.clip = clip;
        _audio.loop = loop;
        _audio.Play();
    }

    void StopSfx()
    {
        if (_audio == null) return;
        _audio.loop = false;
        _audio.Stop();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;
        Bounds b = c.bounds;

        // Green box = detection zone sa ibabaw ng wall
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.4f);
        Gizmos.DrawCube(
            new Vector3(b.center.x, b.max.y + topCheckThickness * 0.5f, 0f),
            new Vector3(b.size.x * topCheckWidth, topCheckThickness, 0f)
        );

        // Orange line = fall path
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 15f);
    }
#endif
}