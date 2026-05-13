using UnityEngine;
using UnityEngine.Playables;

public class TimelineAutoPlay : MonoBehaviour
{
    [SerializeField] PlayableDirector director;

    PlayerMovement playerMovement;
    Rigidbody2D    playerRb;
    Animator       playerAnimator;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerRb       = player.GetComponent<Rigidbody2D>();
            playerAnimator = player.GetComponent<Animator>();
        }

        director.stopped += OnTimelineStopped;

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerRb != null)       playerRb.velocity      = Vector2.zero;
        if (playerAnimator != null) playerAnimator.speed   = 0f;

        director.Play();
    }

    void OnDestroy() => director.stopped -= OnTimelineStopped;

    void OnTimelineStopped(PlayableDirector d)
    {
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAnimator != null) playerAnimator.speed   = 1f;
    }
}
