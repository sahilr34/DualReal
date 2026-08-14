using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DailyRewardManager : MonoBehaviour
{
    [Header("Reward Grid")]
    [SerializeField] private Transform rewardGrid;

    [Header("UI")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text timerText;

    [Header("Reward Settings")]
    [Tooltip("86400 = 24 hours. Use 10 for testing.")]
    [SerializeField] private float rewardIntervalSeconds = 86400f;

    [Header("GameObject References")]
    [SerializeField] private GameObject rewardPanel;

    public bool isTesting=false;

    [Header("Reward Event")]
    [Tooltip("Connect this to your CoinManager.")]
    [SerializeField] private UnityEvent<int> onRewardClaimed;

    private int minRewardAmount = 1;
    private int maxRewardAmount = 5;
    private const int TOTAL_DAYS = 7;

    private const string CURRENT_DAY_KEY = "DailyReward_CurrentDay";
    private const string TIMER_END_KEY = "DailyReward_TimerEnd";

    private const string AMOUNT_KEY_PREFIX = "DailyReward_Amount_";

    private RewardDayUI[] rewardDays;

    private int currentDay;
    private double timerEndTime;

    private bool timerRunning;

    [Serializable]
    private class RewardDayUI
    {
        public Transform root;

        public TMP_Text dayText;
        public TMP_Text rewardAmountText;
        public TMP_Text rewardStatusText;

        public GameObject mysteryImageIcon;
        public GameObject rewardIcon;
    }

    private void Awake()
    {
        InitializeRewardUI();
        LoadDailyRewardData();

        rewardPanel.SetActive(false);

        claimButton.onClick.RemoveListener(ClaimReward);
        claimButton.onClick.AddListener(ClaimReward);
    }
    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        UpdateTimer();
    }

    private void ResetTimer()
    {
        if(isTesting==true)
        {
            PlayerPrefs.DeleteKey(CURRENT_DAY_KEY);
            PlayerPrefs.DeleteKey(AMOUNT_KEY_PREFIX);
            PlayerPrefs.DeleteKey(TIMER_END_KEY);
            PlayerPrefs.Save();
        }
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void InitializeRewardUI()
    {
        if (rewardGrid == null)
        {
            Debug.LogError("DailyRewardManager: Reward Grid is not assigned.");
            return;
        }

        rewardDays = new RewardDayUI[TOTAL_DAYS];

        for (int i = 0; i < TOTAL_DAYS; i++)
        {
            Transform rewardDay = rewardGrid.GetChild(i);

            RewardDayUI rewardUI = new RewardDayUI();

            rewardUI.root = rewardDay;

            rewardUI.dayText =
                FindTMP(rewardDay, "TMP_DayText");

            rewardUI.rewardAmountText =
                FindTMP(rewardDay, "TMP_RewardAmountText");

            rewardUI.rewardStatusText =
                FindTMP(rewardDay, "TMP_RewardStatusText");

            Transform mystery =
                rewardDay.Find("MysteryImageIcon");

            Transform reward =
                rewardDay.Find("RewardIcon");

            if (mystery != null)
                rewardUI.mysteryImageIcon = mystery.gameObject;

            if (reward != null)
                rewardUI.rewardIcon = reward.gameObject;

            rewardDays[i] = rewardUI;

            // Make sure Day text is correct.
            if (rewardUI.dayText != null)
                rewardUI.dayText.text = $"DAY {i + 1}";
        }
    }

    private TMP_Text FindTMP(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);

        if (child == null)
        {
            Debug.LogWarning(
                $"DailyRewardManager: Could not find {objectName} inside {parent.name}");

            return null;
        }

        return child.GetComponent<TMP_Text>();
    }

    // =========================================================
    // LOAD / SAVE
    // =========================================================

    private void LoadDailyRewardData()
    {
        // First time playing.
        if (!PlayerPrefs.HasKey(CURRENT_DAY_KEY))
        {
            currentDay = 1;

            timerRunning = false;
            timerEndTime = 0;

            PlayerPrefs.SetInt(CURRENT_DAY_KEY, currentDay);
            PlayerPrefs.SetString(TIMER_END_KEY, "0");

            PlayerPrefs.Save();
        }
        else
        {
            currentDay =
                PlayerPrefs.GetInt(CURRENT_DAY_KEY, 1);

            timerEndTime =
                double.Parse(
                    PlayerPrefs.GetString(TIMER_END_KEY, "0")
                );

            timerRunning = timerEndTime > GetCurrentUnixTime();
        }

        RefreshAllRewards();

        // If the timer already expired while the game was closed.
        if (timerRunning && GetCurrentUnixTime() >= timerEndTime)
        {
            timerRunning = false;
            timerEndTime = 0;

            SaveTimer();
        }

        RefreshAllRewards();
    }

    private void SaveCurrentDay()
    {
        PlayerPrefs.SetInt(CURRENT_DAY_KEY, currentDay);
        PlayerPrefs.Save();
    }

    private void SaveTimer()
    {
        PlayerPrefs.SetString(
            TIMER_END_KEY,
            timerEndTime.ToString()
        );

        PlayerPrefs.Save();
    }

    // =========================================================
    // REWARD UI
    // =========================================================

    private void RefreshAllRewards()
    {
        if (rewardDays == null)
            return;

        bool currentDayAvailable =
            !timerRunning &&
            currentDay >= 1 &&
            currentDay <= TOTAL_DAYS;

        for (int i = 0; i < TOTAL_DAYS; i++)
        {
            int dayNumber = i + 1;

            RewardDayUI rewardUI = rewardDays[i];

            // -------------------------------------------------
            // ALREADY CLAIMED DAYS
            // -------------------------------------------------

            if (dayNumber < currentDay)
            {
                SetClaimedUI(rewardUI, dayNumber);
            }

            // -------------------------------------------------
            // CURRENT DAY
            // -------------------------------------------------

            else if (dayNumber == currentDay)
            {
                if (currentDayAvailable)
                {
                    SetUnlockedUI(rewardUI, dayNumber);
                }
                else
                {
                    SetLockedUI(rewardUI, dayNumber);
                }
            }

            // -------------------------------------------------
            // FUTURE DAYS
            // -------------------------------------------------

            else
            {
                SetLockedUI(rewardUI, dayNumber);
            }
        }

        UpdateClaimButton();
    }

    // =========================================================
    // CLAIMED
    // =========================================================

    private void SetClaimedUI(
        RewardDayUI rewardUI,
        int dayNumber)
    {
        if (rewardUI.mysteryImageIcon != null)
            rewardUI.mysteryImageIcon.SetActive(false);

        if (rewardUI.rewardIcon != null)
            rewardUI.rewardIcon.SetActive(true);

        if (rewardUI.rewardStatusText != null)
            rewardUI.rewardStatusText.text = "CLAIMED";

        // Display the already generated reward.
        int amount = GetRewardAmount(dayNumber);

        if (rewardUI.rewardAmountText != null)
            rewardUI.rewardAmountText.text = amount.ToString();
    }

    // =========================================================
    // UNLOCKED
    // =========================================================

    private void SetUnlockedUI(
        RewardDayUI rewardUI,
        int dayNumber)
    {
        if (rewardUI.mysteryImageIcon != null)
            rewardUI.mysteryImageIcon.SetActive(false);

        if (rewardUI.rewardIcon != null)
            rewardUI.rewardIcon.SetActive(true);

        if (rewardUI.rewardStatusText != null)
            rewardUI.rewardStatusText.text = "UNLOCKED";

        // Generate reward only once.
        int amount = GetOrCreateRewardAmount(dayNumber);

        if (rewardUI.rewardAmountText != null)
            rewardUI.rewardAmountText.text = amount.ToString();
    }

    // =========================================================
    // LOCKED
    // =========================================================

    private void SetLockedUI(
        RewardDayUI rewardUI,
        int dayNumber)
    {
        if (rewardUI.mysteryImageIcon != null)
            rewardUI.mysteryImageIcon.SetActive(true);

        if (rewardUI.rewardIcon != null)
            rewardUI.rewardIcon.SetActive(false);

        if (rewardUI.rewardStatusText != null)
            rewardUI.rewardStatusText.text = "LOCKED";

        // Do not reveal reward amount.
        if (rewardUI.rewardAmountText != null)
            rewardUI.rewardAmountText.text = "0";
    }

    // =========================================================
    // REWARD GENERATION
    // =========================================================

    private int GetOrCreateRewardAmount(int dayNumber)
    {
        string key =
            AMOUNT_KEY_PREFIX + dayNumber;

        if (!PlayerPrefs.HasKey(key))
        {
            int randomAmount =
                UnityEngine.Random.Range(
                    minRewardAmount,
                    maxRewardAmount + 1
                );

            PlayerPrefs.SetInt(key, randomAmount);
            PlayerPrefs.Save();

            return randomAmount;
        }

        return PlayerPrefs.GetInt(key);
    }

    private int GetRewardAmount(int dayNumber)
    {
        string key =
            AMOUNT_KEY_PREFIX + dayNumber;

        return PlayerPrefs.GetInt(key, 0);
    }

    // =========================================================
    // CLAIM
    // =========================================================

    private void ClaimReward()
    {
        // Cannot claim while timer is running.
        if (timerRunning)
        {
            Debug.Log("Daily Reward is not ready yet.");
            return;
        }

        int rewardAmount =
            GetOrCreateRewardAmount(currentDay);

        Debug.Log(
            $"Daily Reward Claimed: Day {currentDay}, Amount {rewardAmount}"
        );

        // Give reward to CoinManager or another system.
        onRewardClaimed?.Invoke(rewardAmount);

        // Current day is now considered claimed.
        int claimedDay = currentDay;

        currentDay++;

        // -----------------------------------------------------
        // DAY 7 COMPLETED
        // Start a new 7-day cycle.
        // -----------------------------------------------------

        if (currentDay > TOTAL_DAYS)
        {
            currentDay = 1;

            ClearPreviousCycleRewards();
        }

        SaveCurrentDay();

        // Start 24-hour timer.
        timerEndTime =
            GetCurrentUnixTime() + rewardIntervalSeconds;

        timerRunning = true;

        SaveTimer();

        RefreshAllRewards();

        Debug.Log(
            $"Day {claimedDay} claimed. Next reward: Day {currentDay}"
        );
    }

    // =========================================================
    // TIMER
    // =========================================================

    private void UpdateTimer()
    {
        if (!timerRunning)
        {
            timerText.text = "00:00:00";

            UpdateClaimButton();
            return;
        }

        double remaining =
            timerEndTime - GetCurrentUnixTime();

        if (remaining <= 0)
        {
            TimerFinished();
            return;
        }

        TimeSpan time =
            TimeSpan.FromSeconds(remaining);

        timerText.text =
            $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";

        claimButton.interactable = false;
    }

    private void TimerFinished()
    {
        timerRunning = false;
        timerEndTime = 0;

        SaveTimer();

        RefreshAllRewards();

        timerText.text = "00:00:00";

        Debug.Log(
            $"Daily Reward Day {currentDay} is now available!"
        );
    }

    private void UpdateClaimButton()
    {
        if (claimButton == null)
            return;

        // Claim is possible only when:
        // 1. Timer is not running.
        // 2. Current day is valid.

        bool canClaim =
            !timerRunning &&
            currentDay >= 1 &&
            currentDay <= TOTAL_DAYS;

        claimButton.interactable = canClaim;
    }

    // =========================================================
    // NEW CYCLE
    // =========================================================

    private void ClearPreviousCycleRewards()
    {
        for (int i = 1; i <= TOTAL_DAYS; i++)
        {
            string key =
                AMOUNT_KEY_PREFIX + i;

            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
    }

    // =========================================================
    // TIME
    // =========================================================

    private double GetCurrentUnixTime()
    {
        return
            (DateTime.UtcNow -
             new DateTime(
                 1970,
                 1,
                 1,
                 0,
                 0,
                 0,
                 DateTimeKind.Utc
             )).TotalSeconds;
    }

    // =========================================================
    // TESTING
    // =========================================================

    [ContextMenu("Reset Daily Reward")]
    public void ResetDailyReward()
    {
        PlayerPrefs.DeleteKey(CURRENT_DAY_KEY);
        PlayerPrefs.DeleteKey(TIMER_END_KEY);

        for (int i = 1; i <= TOTAL_DAYS; i++)
        {
            PlayerPrefs.DeleteKey(
                AMOUNT_KEY_PREFIX + i
            );
        }

        PlayerPrefs.Save();

        currentDay = 1;
        timerRunning = false;
        timerEndTime = 0;

        RefreshAllRewards();

        Debug.Log("Daily Reward system reset.");
    }
}