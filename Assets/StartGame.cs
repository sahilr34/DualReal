using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    // Name of the level selection scene
    public string levelSceneName = "Level";

    // This function will run when the Start button is clicked
    public void PlayGame()
    {
        SceneManager.LoadScene(levelSceneName);
    }
}
    