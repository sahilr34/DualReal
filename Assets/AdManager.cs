using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

public class AdManager : MonoBehaviour
{
    [Header("LevelPlay Settings")]
    [SerializeField] private string appKey = "27a506cdd";

    [Header("LevelPlay Ad Unit IDs")]
    [SerializeField] private string rewardedAdUnitId = "gdo3fqyhgjr788ct";
    [SerializeField] private string interstitialAdUnitId = "o0qe79c61x30mbdl";
    [SerializeField] private string bannerAdUnitId = "mzb0i9zi6p59dzvz";

    [Header("Ad Frequency")]
    [Range(0f, 1f)]
    public float adChanceOnRestart = 0f;

    public float minTimeBetweenAds = 60f;

    [Header("Banner Settings")]
    public bool showBannerOnMainMenu = true;
    public bool showBannerOnGameOver = true;
    public bool showBannerOnLevels = true;

    public static AdManager Instance;

    private bool isInitialized = false;
    private bool isAdShowing = false;

    private bool isInterstitialAdLoaded = false;
    private bool isRewardedAdLoaded = false;
    private bool isBannerLoaded = false;
    private bool isBannerShowing = false;
    private bool isLoadingBanner = false;

    private float lastAdTime = 0f;

    private bool shouldShowAdOnRestart = false;
    private Action restartCallback;
    private string targetSceneName = "";

    private string currentSceneName = "";
    private bool bannerShouldBeVisible = true;

    private Coroutine bannerRetryCoroutine;

    // LevelPlay ad objects
    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayBannerAd bannerAd;

    // Existing events - ReviveManager ke liye same rakhe hain
    public Action OnAdCompleted;
    public Action OnRewardEarned;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            // Restart ads disabled
            adChanceOnRestart = 0f;

            SceneManager.sceneLoaded += OnSceneLoaded;

            InitializeLevelPlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed -= OnLevelPlayInitFailed;

        if (bannerRetryCoroutine != null)
        {
            StopCoroutine(bannerRetryCoroutine);
        }

        if (bannerAd != null)
        {
            bannerAd.DestroyAd();
        }

        if (interstitialAd != null)
        {
            interstitialAd.DestroyAd();
        }
    }


    // =========================================================
    // LEVELPLAY INITIALIZATION
    // =========================================================

    private void InitializeLevelPlay()
    {
        Debug.Log("Initializing LevelPlay...");

        // Temporary: enable LevelPlay Integration Test Suite for Android testing.
        LevelPlay.SetMetaData("is_test_suite", "enable");

        LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed += OnLevelPlayInitFailed;

        LevelPlay.Init(appKey);
    }


    private void OnLevelPlayInitSuccess(LevelPlayConfiguration configuration)
    {
        Debug.Log("LevelPlay initialized successfully.");

        isInitialized = true;

        // Temporary: launch LevelPlay Integration Test Suite on the Android device.
        LevelPlay.LaunchTestSuite();

        CreateRewardedAd();
        CreateInterstitialAd();
        CreateBannerAd();

        LoadRewardedAd();
        LoadInterstitialAd();
        LoadBannerAd();
    }


    private void OnLevelPlayInitFailed(LevelPlayInitError error)
    {
        Debug.LogError("LevelPlay initialization failed: " + error);

        isInitialized = false;

        StartCoroutine(RetryInitialization());
    }


    private IEnumerator RetryInitialization()
    {
        yield return new WaitForSecondsRealtime(5f);

        if (!isInitialized)
        {
            InitializeLevelPlay();
        }
    }


    // =========================================================
    // SCENE
    // =========================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;

        Debug.Log("Scene Loaded: " + currentSceneName);

        AudioListener.pause = false;

        // Restart callback
        if (shouldShowAdOnRestart && restartCallback != null)
        {
            restartCallback.Invoke();

            restartCallback = null;
            shouldShowAdOnRestart = false;
        }

        UpdateBannerVisibilityForScene(scene.name);
    }


    private void UpdateBannerVisibilityForScene(string sceneName)
    {
        bool shouldShowBanner = false;

        if (sceneName == "Mainmenu" || sceneName == "MainMenu")
        {
            shouldShowBanner = showBannerOnMainMenu;
        }
        else if (
            sceneName == "Gameover" ||
            sceneName == "GameOver" ||
            sceneName == "Game Over")
        {
            shouldShowBanner = showBannerOnGameOver;
        }
        else if (
            sceneName == "Level" ||
            sceneName == "Level1" ||
            sceneName == "Level_1" ||
            sceneName == "Level2" ||
            sceneName == "Level_2" ||
            sceneName == "Level3" ||
            sceneName == "Level_3" ||
            sceneName.StartsWith("Level"))
        {
            shouldShowBanner = showBannerOnLevels;
        }
        else if (
            sceneName == "Youwin" ||
            sceneName == "YouWin" ||
            sceneName == "You Win")
        {
            shouldShowBanner = true;
        }

        bannerShouldBeVisible = shouldShowBanner;

        if (!isInitialized || bannerAd == null)
            return;

        if (shouldShowBanner)
        {
            if (isBannerLoaded && !isBannerShowing)
            {
                ShowBannerAd();
            }
            else if (!isBannerLoaded && !isLoadingBanner)
            {
                LoadBannerAd();
            }
        }
        else
        {
            if (isBannerShowing)
            {
                HideBannerAd();
            }
        }
    }


    // =========================================================
    // REWARDED AD
    // =========================================================

    private void CreateRewardedAd()
    {
        if (rewardedAd != null)
            return;

        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        rewardedAd.OnAdLoaded += RewardedOnAdLoaded;
        rewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailed;
        rewardedAd.OnAdDisplayed += RewardedOnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += RewardedOnAdDisplayFailed;
        rewardedAd.OnAdRewarded += RewardedOnAdRewarded;
        rewardedAd.OnAdClosed += RewardedOnAdClosed;
        rewardedAd.OnAdClicked += RewardedOnAdClicked;
    }


    private void LoadRewardedAd()
    {
        if (!isInitialized || rewardedAd == null)
            return;

        Debug.Log("Loading LevelPlay Rewarded Ad...");

        isRewardedAdLoaded = false;

        rewardedAd.LoadAd();
    }


    public void ShowRewardedAd()
    {
        if (!isInitialized || rewardedAd == null)
        {
            Debug.LogWarning("LevelPlay is not initialized.");
            return;
        }

        if (isAdShowing)
        {
            Debug.Log("Another ad is already showing.");
            return;
        }

        if (!rewardedAd.IsAdReady())
        {
            Debug.Log("Rewarded ad is not ready yet.");
            LoadRewardedAd();
            return;
        }

        isAdShowing = true;

        if (isBannerShowing)
        {
            HideBannerAd();
        }

        Debug.Log("Showing LevelPlay Rewarded Ad...");

        rewardedAd.ShowAd();
    }


    private void RewardedOnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Loaded.");

        isRewardedAdLoaded = true;
    }


    private void RewardedOnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("Rewarded Ad Load Failed: " + error);

        isRewardedAdLoaded = false;

        StartCoroutine(RetryRewardedAd());
    }


    private IEnumerator RetryRewardedAd()
    {
        yield return new WaitForSecondsRealtime(5f);

        if (isInitialized && !isRewardedAdLoaded)
        {
            LoadRewardedAd();
        }
    }


    private void RewardedOnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Displayed.");
    }


    private void RewardedOnAdDisplayFailed(
        LevelPlayAdInfo adInfo,
        LevelPlayAdError error)
    {
        Debug.LogError("Rewarded Ad Display Failed: " + error);

        isAdShowing = false;

        RestoreBanner();

        LoadRewardedAd();
    }


    private void RewardedOnAdRewarded(
        LevelPlayAdInfo adInfo,
        LevelPlayReward reward)
    {
        Debug.Log(
            "Reward received: " +
            reward.Name +
            " - " +
            reward.Amount
        );

        // IMPORTANT:
        // Ye tumhare existing ReviveManager ke
        // OnRewardEarned event ko trigger karega.
        OnRewardEarned?.Invoke();
    }


    private void RewardedOnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Closed.");

        isAdShowing = false;
        isRewardedAdLoaded = false;

        RestoreBanner();

        LoadRewardedAd();
    }


    private void RewardedOnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Clicked.");
    }


    // =========================================================
    // INTERSTITIAL AD
    // =========================================================

    private void CreateInterstitialAd()
    {
        if (interstitialAd != null)
            return;

        interstitialAd =
            new LevelPlayInterstitialAd(interstitialAdUnitId);

        interstitialAd.OnAdLoaded += InterstitialOnAdLoaded;
        interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailed;
        interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayed;
        interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailed;
        interstitialAd.OnAdClicked += InterstitialOnAdClicked;
        interstitialAd.OnAdClosed += InterstitialOnAdClosed;
    }


    private void LoadInterstitialAd()
    {
        if (!isInitialized || interstitialAd == null)
            return;

        Debug.Log("Loading LevelPlay Interstitial Ad...");

        isInterstitialAdLoaded = false;

        interstitialAd.LoadAd();
    }


    public void ShowInterstitialAd()
    {
        if (!isInitialized ||
            interstitialAd == null ||
            isAdShowing)
        {
            CompleteRestartWithoutAd();
            return;
        }

        if (!interstitialAd.IsAdReady())
        {
            Debug.Log("Interstitial ad is not ready.");

            CompleteRestartWithoutAd();
            LoadInterstitialAd();

            return;
        }

        isAdShowing = true;

        if (isBannerShowing)
        {
            HideBannerAd();
        }

        Time.timeScale = 0f;
        AudioListener.pause = true;

        Debug.Log("Showing LevelPlay Interstitial Ad...");

        interstitialAd.ShowAd();
    }


    private void InterstitialOnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Loaded.");

        isInterstitialAdLoaded = true;
    }


    private void InterstitialOnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("Interstitial Ad Load Failed: " + error);

        isInterstitialAdLoaded = false;

        StartCoroutine(RetryInterstitialAd());
    }


    private IEnumerator RetryInterstitialAd()
    {
        yield return new WaitForSecondsRealtime(5f);

        if (isInitialized && !isInterstitialAdLoaded)
        {
            LoadInterstitialAd();
        }
    }


    private void InterstitialOnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Displayed.");
    }


    private void InterstitialOnAdDisplayFailed(
        LevelPlayAdInfo adInfo,
        LevelPlayAdError error)
    {
        Debug.LogError("Interstitial Display Failed: " + error);

        isAdShowing = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        RestoreBanner();

        CompleteRestartAfterAd();

        LoadInterstitialAd();
    }


    private void InterstitialOnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Clicked.");
    }


    private void InterstitialOnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Closed.");

        isAdShowing = false;
        isInterstitialAdLoaded = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        lastAdTime = Time.unscaledTime;

        RestoreBanner();

        CompleteRestartAfterAd();

        OnAdCompleted?.Invoke();

        LoadInterstitialAd();
    }


    // =========================================================
    // BANNER AD
    // =========================================================

    private void CreateBannerAd()
    {
        if (bannerAd != null)
            return;

        var configBuilder =
            new LevelPlayBannerAd.Config.Builder();

        configBuilder.SetPosition(
            LevelPlayBannerPosition.BottomCenter
        );

        configBuilder.SetDisplayOnLoad(false);

        var bannerConfig = configBuilder.Build();

        bannerAd =
            new LevelPlayBannerAd(
                bannerAdUnitId,
                bannerConfig
            );

        bannerAd.OnAdLoaded += BannerOnAdLoaded;
        bannerAd.OnAdLoadFailed += BannerOnAdLoadFailed;
        bannerAd.OnAdDisplayed += BannerOnAdDisplayed;
        bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailed;
        bannerAd.OnAdClicked += BannerOnAdClicked;
    }


    public void LoadBannerAd()
    {
        if (!isInitialized || bannerAd == null)
            return;

        if (isBannerLoaded)
        {
            if (bannerShouldBeVisible && !isBannerShowing)
            {
                ShowBannerAd();
            }

            return;
        }

        if (isLoadingBanner)
            return;

        isLoadingBanner = true;

        Debug.Log("Loading LevelPlay Banner Ad...");

        bannerAd.LoadAd();
    }


    private void BannerOnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Loaded.");

        isBannerLoaded = true;
        isLoadingBanner = false;

        if (bannerShouldBeVisible)
        {
            ShowBannerAd();
        }
    }


    private void BannerOnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("Banner Ad Load Failed: " + error);

        isBannerLoaded = false;
        isLoadingBanner = false;

        if (bannerRetryCoroutine != null)
        {
            StopCoroutine(bannerRetryCoroutine);
        }

        bannerRetryCoroutine =
            StartCoroutine(RetryLoadBanner());
    }


    private IEnumerator RetryLoadBanner()
    {
        yield return new WaitForSecondsRealtime(5f);

        if (isInitialized &&
            !isBannerLoaded &&
            bannerShouldBeVisible)
        {
            LoadBannerAd();
        }
    }


    public void ShowBannerAd()
    {
        if (!isInitialized || bannerAd == null)
            return;

        if (!bannerShouldBeVisible)
            return;

        if (!isBannerLoaded)
        {
            LoadBannerAd();
            return;
        }

        if (isBannerShowing)
            return;

        Debug.Log("Showing LevelPlay Banner Ad...");

        bannerAd.ShowAd();

        isBannerShowing = true;
    }


    public void HideBannerAd()
    {
        if (bannerAd == null)
            return;

        Debug.Log("Hiding LevelPlay Banner Ad...");

        bannerAd.HideAd();

        isBannerShowing = false;
    }


    public void DestroyBannerAd()
    {
        if (bannerAd == null)
            return;

        Debug.Log("Destroying LevelPlay Banner Ad...");

        bannerAd.DestroyAd();

        bannerAd = null;

        isBannerLoaded = false;
        isBannerShowing = false;
        isLoadingBanner = false;
    }


    private void BannerOnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Displayed.");

        isBannerShowing = true;
    }


    private void BannerOnAdDisplayFailed(
        LevelPlayAdInfo adInfo,
        LevelPlayAdError error)
    {
        Debug.LogError("Banner Display Failed: " + error);

        isBannerShowing = false;
    }


    private void BannerOnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Clicked.");
    }


    private void RestoreBanner()
    {
        if (bannerShouldBeVisible && isBannerLoaded)
        {
            ShowBannerAd();
        }
    }


    // =========================================================
    // RESTART SYSTEM
    // =========================================================

    public void RequestRestartWithAd(
        Action callback,
        string sceneName = "")
    {
        restartCallback = callback;
        targetSceneName = sceneName;

        float timeSinceLastAd =
            Time.unscaledTime - lastAdTime;

        if (timeSinceLastAd >= minTimeBetweenAds)
        {
            float randomValue =
                UnityEngine.Random.Range(0f, 1f);

            if (randomValue <= adChanceOnRestart &&
                isInterstitialAdLoaded)
            {
                shouldShowAdOnRestart = true;

                ShowInterstitialAd();

                return;
            }
        }

        OnAdCompleted?.Invoke();

        shouldShowAdOnRestart = false;

        callback?.Invoke();
    }


    public void RequestRestartWithAd(Action callback)
    {
        RequestRestartWithAd(callback, "");
    }


    private void CompleteRestartAfterAd()
    {
        if (shouldShowAdOnRestart &&
            restartCallback != null)
        {
            restartCallback.Invoke();

            restartCallback = null;
            shouldShowAdOnRestart = false;
        }
    }


    private void CompleteRestartWithoutAd()
    {
        shouldShowAdOnRestart = false;

        if (restartCallback != null)
        {
            restartCallback.Invoke();
            restartCallback = null;
        }

        OnAdCompleted?.Invoke();
    }
}