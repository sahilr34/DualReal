
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReviveManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject revivePanel;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button noThanksButton;
    [SerializeField] private Button watchAdButton;

    [Header("Settings")]
    [SerializeField] private float reviveTimer = 10f;

    private float timer;
    private bool rewardReceived;
    private bool timerRunning;

    private void Awake()
    {
        if (noThanksButton != null)
            noThanksButton.onClick.AddListener(NoThanks);

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(WatchAd);

        HidePanel();
    }

    private void OnDestroy()
    {
        if (noThanksButton != null)
            noThanksButton.onClick.RemoveListener(NoThanks);

        if (watchAdButton != null)
            watchAdButton.onClick.RemoveListener(WatchAd);

        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardEarned -=
                OnRewardEarned;
        }
    }

    public void ShowPanel()
    {
        if (revivePanel != null)
            revivePanel.SetActive(true);

        timer = reviveTimer;
        rewardReceived = false;
        timerRunning = true;

        UpdateTimerText();

        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardEarned -=
                OnRewardEarned;

            AdManager.Instance.OnRewardEarned +=
                OnRewardEarned;
        }
    }

    public void HidePanel()
    {
        timerRunning = false;

        if (revivePanel != null)
            revivePanel.SetActive(false);
    }

    private void Update()
    {
        if (!timerRunning || rewardReceived)
            return;

        // Time.timeScale = 0 during revive. 
        // Therefore use unscaledDeltaTime. 
        timer -= Time.unscaledDeltaTime;

        UpdateTimerText();

        if (timer <= 0f)
        {
            timerRunning = false;

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ShowGameOver();
            }
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text =
                Mathf.Max(0, Mathf.CeilToInt(timer)).ToString();
        }
    }

    public void NoThanks()
    {
        if (!timerRunning)
            return;

        timerRunning = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ShowGameOver();
        }
    }

    public void WatchAd()
    {
        if (rewardReceived)
            return;

        // Immediately stop countdown.
        timerRunning = false;

        // Prevent timeout logic completely.
        timer = 0f;

        Debug.Log("Revive ad requested. Timer stopped.");

        if (AdManager.Instance != null)
        {
            AdManager.Instance.ShowRewardedAd();
        }
        else
        {
            OnRewardEarned();
        }
    }

    private void OnRewardEarned()
    {
        if (rewardReceived)
            return;

        rewardReceived = true;
        timerRunning = false;

        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardEarned -=
                OnRewardEarned;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RevivePlayer();
        }
    }
}
