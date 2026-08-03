using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Called when Start button is pressed
    public void StartGame()
    {
        SceneManager.LoadScene("Level"); // Replace with your game scene name
    }

    // Called when Level button is pressed
    public void LoadLevelSelect()
    {
        SceneManager.LoadScene("Level"); // Replace with your level select scene name
    }

    public void LoadSetting()
    {
        SceneManager.LoadScene("Setting"); // Replace with your setting select scene name
    }

    // Called when Exit button is pressed
    public void ExitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit(); // Only works in build
    }
}
