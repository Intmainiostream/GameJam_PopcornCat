using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] GameObject levelCompleteUI;
    [SerializeField] string nextSceneName;
    [SerializeField] bool isLastLevel;

    public void GoalReached()
    {
        if (isLastLevel)
        {
            PlayerPrefs.SetInt("AllLevelsCompleted", 1);
            StartCoroutine(LoadMainMenuAfterDelay());
            return;
        }

        levelCompleteUI.SetActive(true);
        Time.timeScale = 0f;
    }

    IEnumerator LoadMainMenuAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("MainMenu");
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
