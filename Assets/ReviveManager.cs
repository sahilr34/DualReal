using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ReviveManager : MonoBehaviour
{
    [Header("UI")]
    public Text timerText;

    [Header("Settings")]
    public float reviveTimer = 10f;

    private float timer;
    private bool rewardReceived = false;

    private void OnEnable()
    {
        timer = reviveTimer;
        rewardReceived = false;

        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardEarned += OnRewardEarned;
        }
    }

    private void OnDisable()
    {
        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardEarned -= OnRewardEarned;
        }
    }

    private void Update()
    {
        if (rewardReceived)
            return;

        timer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(timer).ToString();
        }

        if (timer <= 0f)
        {
            SceneManager.LoadScene("GameOver");
        }
    }

    public void NoThanks()
    {
        SceneManager.LoadScene("GameOver");
    }

    public void WatchAd()
    {
        if (AdManager.Instance != null)
        {
            AdManager.Instance.ShowRewardedAd();
        }
    }

    private void OnRewardEarned()
    {
        if (rewardReceived)
            return;

        rewardReceived = true;
        GameState.reviveUsed = true;

        SceneManager.LoadScene(GameState.lastGameScene);
    }
}