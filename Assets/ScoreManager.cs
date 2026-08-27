using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    public Text scoreText;
    public Text targetScoreText;

    [Header("Score Settings")]
    private int score = 0;
    private bool isScoring = false;

    private int nextAdScore = 40;

    [Header("Win Condition")]
    public int targetScore = 40;

    public string youWinSceneName = "YouWin";
    public string chaseSceneName = "Chase";
    public string endlessSceneName = "Endless";

    private bool allowWinCondition = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(AddScore));

        if (Instance == this)
        {
            Instance = null;
        }
    }

   

    private void Start()
    {
        score = 0;
        nextAdScore = 40;

        string currentScene =
            SceneManager.GetActiveScene().name;

        SetTargetScoreForCurrentScene(currentScene);

        UpdateScoreText();

        StartScoring();
    }

   


    private void SetTargetScoreForCurrentScene(
        string sceneName)
    {
        allowWinCondition =
            sceneName == chaseSceneName;

        if (allowWinCondition)
        {
            targetScore =
                Random.Range(100, 160);
        }

        UpdateScoreText();
    }

    

    private void StartScoring()
    {
        isScoring = true;

        CancelInvoke(nameof(AddScore));

        InvokeRepeating(
            nameof(AddScore),
            1f,
            1f);
    }

    public void ResetScore()
    {
        score = 0;
        nextAdScore = 40;

        isScoring = false;

        CancelInvoke(nameof(AddScore));

        UpdateScoreText();
    }

    private void AddScore()
    {
        if (!isScoring)
            return;

        score++;

        UpdateScoreText();

        if (score >= nextAdScore)
        {
            ShowAd();

            nextAdScore += 40;
        }

        if (allowWinCondition &&
            score >= targetScore)
        {
            WinGame();
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Score: " + score;
        }

        if (targetScoreText != null)
        {
            targetScoreText.text =
                "Target: " + targetScore;
        }
    }

    private void WinGame()
    {
        isScoring = false;

        CancelInvoke(nameof(AddScore));

        PlayerPrefs.SetInt(
            "FinalScore",
            score);

        PlayerPrefs.Save();

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            youWinSceneName);
    }

    public void StopAndSaveScore()
    {
        isScoring = false;

        CancelInvoke(nameof(AddScore));

        PlayerPrefs.SetInt(
            "FinalScore",
            score);

        PlayerPrefs.Save();
    }

    private void ShowAd()
    {
        if (AdManager.Instance != null)
        {
            AdManager.Instance.ShowInterstitialAd();

            Debug.Log(
                "Showing Interstitial Ad at score: "
                + score);
        }
    }

    public void PauseScoring()
    {
        isScoring = false;

        CancelInvoke(nameof(AddScore));
    }

    public void ResumeScoring()
    {
        if (isScoring)
            return;

        StartScoring();
    }

    public int GetCurrentScore()
    {
        return score;
    }
}
