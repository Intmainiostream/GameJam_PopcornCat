using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] string nextSceneName;
    [SerializeField] bool isLastLevel;

    public void GoalReached()
    {
        if (isLastLevel)
        {
            PlayerPrefs.SetInt("AllLevelsCompleted", 1);
            StartCoroutine(LoadSceneAfterDelay("MainMenu"));
            return;
        }

        StartCoroutine(LoadSceneAfterDelay(nextSceneName));
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
