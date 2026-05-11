using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    [SerializeField] GameObject deathUI;
    [SerializeField] float fallThreshold = -10f;

    bool isDead;

    void Update()
    {
        if (!isDead && transform.position.y < fallThreshold)
            Die();
    }

    public void Die()
    {
        isDead = true;
        deathUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
