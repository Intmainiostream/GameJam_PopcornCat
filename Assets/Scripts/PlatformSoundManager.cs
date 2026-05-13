using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlatformSoundManager : MonoBehaviour
{
    [SerializeField] AudioClip timedPlatformBlinkSfx;
    [SerializeField] AudioClip fallingWallShakeSfx;
    [SerializeField] [Range(0f, 1f)] float blinkVolume     = 1f;
    [SerializeField] [Range(0f, 1f)] float fallShakeVolume = 1f;

    AudioSource audioSource;

    void Awake()
    {
        audioSource             = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop        = false;
    }

    void OnEnable()
    {
        TimedPlatform.OnBlinkStarted    += PlayBlink;
        FallingWall.OnFallShakeStarted  += PlayFallShake;
    }

    void OnDisable()
    {
        TimedPlatform.OnBlinkStarted    -= PlayBlink;
        FallingWall.OnFallShakeStarted  -= PlayFallShake;
    }

    void PlayBlink()     => audioSource.PlayOneShot(timedPlatformBlinkSfx,  blinkVolume);
    void PlayFallShake() => audioSource.PlayOneShot(fallingWallShakeSfx, fallShakeVolume);
}
