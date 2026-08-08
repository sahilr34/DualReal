using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    public string endlessSceneName = "Endless";
    public string chaseSceneName = "Chase";
    public string mmSceneName = "Main Menu";


    public void PlayEndless()
    {
        GameState.reviveUsed = false;
        GameState.savedScore = 0;
        GameState.lastGameScene = "";

        SceneManager.LoadScene(endlessSceneName);
    }

    public void PlayChase()
    {
        GameState.reviveUsed = false;
        GameState.savedScore = 0;
        GameState.lastGameScene = "";

        SceneManager.LoadScene(chaseSceneName);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mmSceneName);
    }


}
