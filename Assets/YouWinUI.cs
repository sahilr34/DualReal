using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class YouWinUI : MonoBehaviour
{
    public Button restartButton;

    public string gameSceneName = "GameScene";

    private void Start()
    {
        restartButton.onClick.AddListener(RestartGame);
    }

    void RestartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}