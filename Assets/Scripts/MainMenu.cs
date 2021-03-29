using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    public void PlayGame()
    {
        var nextLevel = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextLevel);
    }

    public void QuitGame()
    {
        Debug.Log("The player pressed the quit button.");
        Application.Quit();
    }
}
