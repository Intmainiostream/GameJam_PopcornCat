using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] string nextSceneName;
    [SerializeField] bool isLastLevel;
    [SerializeField] PlayableDirector endingTimeline;
    [SerializeField] AudioClip endingSound;
    [SerializeField] GameObject blackBg;
    [SerializeField] float blackBgDelay = 1.5f;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void LockPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var pm = player.GetComponent<PlayerMovement>();
        var rb = player.GetComponent<Rigidbody2D>();
        var an = player.GetComponent<Animator>();
        if (pm != null) pm.enabled     = false;
        if (rb != null) rb.velocity    = Vector2.zero;
        if (an != null) an.speed       = 0f;
    }

    public void GoalReached()
    {
        if (isLastLevel)
        {
            PlayerPrefs.SetInt("AllLevelsCompleted", 1);
            if (endingTimeline != null)
            {
                LockPlayer();
                if (endingSound != null) audioSource.PlayOneShot(endingSound);
                endingTimeline.stopped += _ => StartCoroutine(ShowBlackThenLoad());
                endingTimeline.Play();
            }
            else
            {
                StartCoroutine(LoadSceneAfterDelay("MainMenu"));
            }
            return;
        }

        StartCoroutine(LoadSceneAfterDelay(nextSceneName));
    }

    IEnumerator ShowBlackThenLoad()
    {
        if (blackBg != null) blackBg.SetActive(true);
        yield return new WaitForSeconds(blackBgDelay);
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(sceneName);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
