using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip hoverEffect;

    public void PlayGame()
    {
        var nextLevel = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextLevel);
    }

    public void HoverSound()
    {
        // audioSource = GetComponent<AudioSource>();
        var effect = hoverEffect;
        audioSource.PlayOneShot(hoverEffect);
    }

    public void QuitGame()
    {
        Debug.Log("The player pressed the quit button.");
        Application.Quit();
    }
}
