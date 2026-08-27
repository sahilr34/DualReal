using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;

    [SerializeField] private TMP_Text finalScoreText;

    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private bool rewardClaimed = false;

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(
                OnRestartButtonClick
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(
                GoToMainMenu
            );
        }

        HidePanel();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(
                OnRestartButtonClick
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(
                GoToMainMenu
            );
        }

        UnsubscribeFromAd();
    }

    private void SubscribeToAd()
    {
        if (AdManager.Instance == null)
            return;

        AdManager.Instance.OnRewardEarned -= DoubleScore;
        AdManager.Instance.OnRewardEarned += DoubleScore;
    }

    private void UnsubscribeFromAd()
    {
        if (AdManager.Instance == null)
            return;

        AdManager.Instance.OnRewardEarned -= DoubleScore;
    }

    public void ShowPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        rewardClaimed = false;

        UpdateFinalScore();

        SubscribeToAd();
    }

    public void HidePanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UnsubscribeFromAd();
    }

    private void UpdateFinalScore()
    {
        int finalScore = 0;

        if (ScoreManager.Instance != null)
        {
            finalScore =
                ScoreManager.Instance.GetCurrentScore();
        }

        if (finalScoreText != null)
        {
            finalScoreText.text =
                finalScore.ToString();
        }
    }

    private void OnRestartButtonClick()
    {
        /*
         * Immediately remove our ad callback.
         *
         * This is important because this GameOverUI
         * is about to be destroyed.
         */
        UnsubscribeFromAd();

        if (AdManager.Instance != null)
        {
            AdManager.Instance.RequestRestartWithAd(
                RestartGame
            );
        }
        else
        {
            RestartGame();
        }
    }

    private void RestartGame()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RestartCurrentScene();
        }
        else
        {
            Debug.LogError(
                "GameFlowManager.Instance is NULL!"
            );
        }
    }

    private void GoToMainMenu()
    {
        UnsubscribeFromAd();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.GoToMainMenu();
        }
    }

    private void DoubleScore()
    {
        if (rewardClaimed)
            return;

        rewardClaimed = true;

        UnsubscribeFromAd();

        int currentScore = 0;

        if (ScoreManager.Instance != null)
        {
            currentScore =
                ScoreManager.Instance.GetCurrentScore();
        }

        int doubledScore =
            currentScore * 2;

        PlayerPrefs.SetInt(
            "FinalScore",
            doubledScore
        );

        PlayerPrefs.Save();

        if (finalScoreText != null)
        {
            finalScoreText.text =
                doubledScore.ToString();
        }

        Debug.Log(
            "Score doubled after watching ad."
        );
    }
}