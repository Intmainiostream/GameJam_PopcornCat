using System.Collections;
using UnityEngine;

public class WorldMusic : MonoBehaviour
{
    [SerializeField] AudioClip colorMusic;
    [SerializeField] AudioClip monoMusic;
    [SerializeField] float fadeSpeed = 1.5f;

    AudioSource colorSource;
    AudioSource monoSource;
    Coroutine fadeRoutine;

    void Awake()
    {
        colorSource = CreateSource(colorMusic);
        monoSource  = CreateSource(monoMusic);

        colorSource.volume = 1f;
        colorSource.Play();

        monoSource.volume = 0f;
        monoSource.Play();
    }

    void OnEnable()  => PlayerMovement.OnWorldToggle += OnWorldToggle;
    void OnDisable() => PlayerMovement.OnWorldToggle -= OnWorldToggle;

    void OnWorldToggle(bool isMonochrome)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossFade(isMonochrome ? monoSource : colorSource,
                                               isMonochrome ? colorSource : monoSource));
    }

    IEnumerator CrossFade(AudioSource fadeIn, AudioSource fadeOut)
    {
        while (fadeOut.volume > 0f || fadeIn.volume < 1f)
        {
            fadeOut.volume = Mathf.MoveTowards(fadeOut.volume, 0f, Time.deltaTime * fadeSpeed);
            fadeIn.volume  = Mathf.MoveTowards(fadeIn.volume,  1f, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }

    AudioSource CreateSource(AudioClip clip)
    {
        AudioSource src  = gameObject.AddComponent<AudioSource>();
        src.clip         = clip;
        src.loop         = true;
        src.playOnAwake  = false;
        src.volume       = 0f;
        return src;
    }
}
