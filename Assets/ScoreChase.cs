using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreChase : MonoBehaviour
{
    public int targetScore = 10;
    public string youWinSceneName = "YouWin";

    private int currentScore;

    private void Start()
    {
        currentScore = PlayerPrefs.GetInt("FinalScore", 0);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        PlayerPrefs.SetInt("FinalScore", currentScore);

        Debug.Log("Score: " + currentScore);

        if (currentScore >= targetScore)
        {
            SceneManager.LoadScene(youWinSceneName);
        }
    }
}
