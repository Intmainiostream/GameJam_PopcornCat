using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class TimelineTrigger : MonoBehaviour
{
    [SerializeField] PlayableDirector director;
    [SerializeField] bool playOnce = true;
    [SerializeField] string loadSceneOnEnd;
    [SerializeField] PlayableDirector[] disableOnPlay;

    bool played;
    PlayerMovement playerMovement;

    Rigidbody2D playerRb;
    Animator playerAnimator;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerRb       = player.GetComponent<Rigidbody2D>();
            playerAnimator = player.GetComponent<Animator>();
        }
    }

    void OnEnable()  => director.stopped += OnTimelineStopped;
    void OnDisable() => director.stopped -= OnTimelineStopped;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playOnce && played) return;

        played = true;
        foreach (var d in disableOnPlay) if (d != null) d.gameObject.SetActive(false);
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerRb != null) playerRb.velocity = Vector2.zero;
        if (playerAnimator != null) playerAnimator.speed = 0f;
        director.Play();
    }

    void OnTimelineStopped(PlayableDirector d)
    {
        if (!string.IsNullOrEmpty(loadSceneOnEnd)) { SceneManager.LoadScene(loadSceneOnEnd); return; }
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAnimator != null) playerAnimator.speed = 1f;
    }
}
