using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject colorBg;
    [SerializeField] GameObject monoBg;

    void Start()
    {
        bool allDone = PlayerPrefs.GetInt("AllLevelsCompleted", 0) == 1;
        colorBg.SetActive(!allDone);
        monoBg.SetActive(allDone);
    }

    public void Play()
    {
        SceneManager.LoadScene("Level1Scene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
