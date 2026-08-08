using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public TMP_Text finalScoreText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Scene Names")]
    public string gameSceneName = "Level1";
    public string mainMenuSceneName = "Mainmenu";

    private int finalScore;
    private bool rewardClaimed = false;

    private void Start()
    {
        finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        finalScoreText.text = finalScore.ToString();

        restartButton.onClick.AddListener(OnRestartButtonClick);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
    

        // Subscribe to reward event
        if (AdManager.Instance != null)
            AdManager.Instance.OnRewardEarned += DoubleScore;
    }

    private void OnDestroy()
    {
        if (AdManager.Instance != null)
            AdManager.Instance.OnRewardEarned -= DoubleScore;
    }

    private void OnRestartButtonClick()
    {
        // AdManager से restart request करें (ad show हो सकता है)
        if (AdManager.Instance != null)
        {
            AdManager.Instance.RequestRestartWithAd(RestartGame);
        }
        else
        {
            // अगर AdManager नहीं है तो सीधे restart करें
            RestartGame();
        }
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowRewardedAd()
    {
        if (AdManager.Instance != null)
        {
            AdManager.Instance.ShowRewardedAd();
        }
    }

    private void DoubleScore()
    {
        if (rewardClaimed) return; // prevent multiple rewards

        rewardClaimed = true;
        finalScore *= 2;
        PlayerPrefs.SetInt("FinalScore", finalScore);
        finalScoreText.text = finalScore.ToString();

        Debug.Log("🎯 Score doubled after watching ad!");
    }
}