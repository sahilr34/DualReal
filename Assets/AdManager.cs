using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using System.Collections;

public class AdManager : MonoBehaviour
{
    [Header("AdMob App IDs")]
    [SerializeField] private string androidAppId = "ca-app-pub-9156600449659789~8033456119";

    [Header("AdMob Ad Unit IDs (Android)")]
    [SerializeField] private string admobRewardedAdUnitId = "ca-app-pub-9156600449659789/4005068429";
    [SerializeField] private string admobInterstitialAdUnitId = "ca-app-pub-9156600449659789/2712951877";
    [SerializeField] private string admobBannerAdUnitId = "ca-app-pub-9156600449659789/9169765195";

    [Header("Ad Frequency")]
    [Range(0f, 1f)]
    public float adChanceOnRestart = 0.5f;
    public float minTimeBetweenAds = 60f;

    [Header("Banner Settings")]
    public bool showBannerOnMainMenu = true;
    public bool showBannerOnGameOver = true;
    public bool showBannerOnLevels = true;

    [Header("Banner Refresh Settings")]
    [SerializeField] private float bannerRefreshInterval = 30f;

    [Header("Test Mode")]
    [Tooltip("Use test ads (recommended during development)")]
    public bool useTestAds = false;

    public static AdManager Instance;

    // =========================================================
    // STATE
    // =========================================================

    private bool isInitialized = false;
    private bool isAdShowing = false;
    private bool isRewardClaimed = false;
    private bool isLoadingRewardedAd = false;

    // AdMob Ads
    private RewardedAd admobRewardedAd;
    private InterstitialAd admobInterstitialAd;
    private BannerView admobBannerView;

    // Load States
    private bool isRewardedAdLoaded = false;
    private bool isInterstitialAdLoaded = false;
    private bool isBannerLoaded = false;
    private bool isBannerShowing = false;
    private bool isLoadingBanner = false;

    // Banner Refresh
    private Coroutine bannerRefreshCoroutine;

    private float lastAdTime = 0f;
    private bool shouldShowAdOnRestart = false;
    private Action restartCallback;
    private string currentSceneName = "";
    private bool bannerShouldBeVisible = true;
    private Coroutine bannerRetryCoroutine;

    // Events
    public Action OnAdCompleted;
    public Action OnRewardEarned;
    public Action OnRewardReset;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            InitializeAdMob();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (bannerRetryCoroutine != null)
            StopCoroutine(bannerRetryCoroutine);

        if (bannerRefreshCoroutine != null)
            StopCoroutine(bannerRefreshCoroutine);

        admobBannerView?.Destroy();
        admobRewardedAd?.Destroy();
        admobInterstitialAd?.Destroy();
    }

    // =========================================================
    // ADMOB INITIALIZATION
    // =========================================================

    private void InitializeAdMob()
    {
        Debug.Log("Initializing Google AdMob...");

        MobileAds.Initialize((InitializationStatus status) =>
        {
            Debug.Log($"✅ Google AdMob initialized successfully.");
            isInitialized = true;
            
            isRewardClaimed = false;
            isAdShowing = false;
            
            CreateAdMobAds();
            LoadAdMobAds();
        });
    }

    private string GetAppId()
    {
        if (useTestAds)
        {
            Debug.Log("🔬 Test Mode Enabled - Using Google Test App IDs");
            return "ca-app-pub-3940256099942544~3347511713";
        }
        return androidAppId;
    }

    private string GetInterstitialAdUnitId()
    {
        if (useTestAds)
        {
            return "ca-app-pub-3940256099942544/1033173712";
        }
        return admobInterstitialAdUnitId;
    }

    private string GetRewardedAdUnitId()
    {
        if (useTestAds)
        {
            return "ca-app-pub-3940256099942544/5224354917";
        }
        return admobRewardedAdUnitId;
    }

    private string GetBannerAdUnitId()
    {
        if (useTestAds)
        {
            return "ca-app-pub-3940256099942544/6300978111";
        }
        return admobBannerAdUnitId;
    }

    // =========================================================
    // CREATE ADS
    // =========================================================

    private void CreateAdMobAds()
    {
        if (admobBannerView == null)
        {
            string bannerAdUnitId = GetBannerAdUnitId();
            admobBannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
            admobBannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("✅ AdMob Banner Loaded");
                isBannerLoaded = true;
                isLoadingBanner = false;
                if (bannerShouldBeVisible)
                    ShowBannerAd();
            };
            admobBannerView.OnBannerAdLoadFailed += (error) =>
            {
                Debug.LogError($"❌ AdMob Banner Load Failed: {error}");
                isBannerLoaded = false;
                isLoadingBanner = false;
                StartCoroutine(RetryLoadBanner());
            };
            admobBannerView.OnAdClicked += () => Debug.Log("AdMob Banner Clicked");
            
            AdRequest request = new AdRequest();
            admobBannerView.LoadAd(request);
        }
    }

    private void LoadAdMobAds()
    {
        LoadAdMobRewardedAd();
        LoadAdMobInterstitialAd();
    }

    // =========================================================
    // LOAD REWARDED AD
    // =========================================================

    private void LoadAdMobRewardedAd()
    {
        // Prevent multiple simultaneous loads
        if (isLoadingRewardedAd)
        {
            Debug.Log("⏳ Rewarded ad is already loading...");
            return;
        }

        // Clean up old ad
        if (admobRewardedAd != null)
        {
            admobRewardedAd.Destroy();
            admobRewardedAd = null;
        }

        isLoadingRewardedAd = true;
        isRewardedAdLoaded = false;

        string adUnitId = GetRewardedAdUnitId();
        Debug.Log($"🔄 Loading Rewarded Ad: {adUnitId}");
        
        AdRequest request = new AdRequest();
        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            isLoadingRewardedAd = false;

            if (error != null)
            {
                Debug.LogError($"❌ AdMob Rewarded Load Failed: {error}");
                isRewardedAdLoaded = false;
                StartCoroutine(RetryRewardedLoad(5f));
                return;
            }
            if (ad == null)
            {
                Debug.LogError("❌ AdMob Rewarded Ad is null");
                isRewardedAdLoaded = false;
                return;
            }

            admobRewardedAd = ad;
            isRewardedAdLoaded = true;
            Debug.Log($"✅ AdMob Rewarded Ad Loaded: {adUnitId}");

            // Clear old event handlers to prevent duplicates
            admobRewardedAd.OnAdFullScreenContentOpened -= OnRewardedOpened;
            admobRewardedAd.OnAdFullScreenContentClosed -= OnRewardedClosed;
            admobRewardedAd.OnAdFullScreenContentFailed -= OnRewardedFailed;
            admobRewardedAd.OnAdClicked -= OnRewardedClicked;
            admobRewardedAd.OnAdImpressionRecorded -= OnRewardedImpression;

            // Add new event handlers
            admobRewardedAd.OnAdFullScreenContentOpened += OnRewardedOpened;
            admobRewardedAd.OnAdFullScreenContentClosed += OnRewardedClosed;
            admobRewardedAd.OnAdFullScreenContentFailed += OnRewardedFailed;
            admobRewardedAd.OnAdClicked += OnRewardedClicked;
            admobRewardedAd.OnAdImpressionRecorded += OnRewardedImpression;
        });
    }

    private void OnRewardedOpened()
    {
        Debug.Log("📱 Rewarded Ad Opened");
        isAdShowing = true;
    }

    private void OnRewardedClosed()
    {
        Debug.Log("📱 Rewarded Ad Closed");
        isAdShowing = false;
        isRewardedAdLoaded = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        // Restore banner
        if (bannerShouldBeVisible && isBannerLoaded)
        {
            ShowBannerAd();
        }
        
        // Always reload for next use
        LoadAdMobRewardedAd();
    }

    private void OnRewardedFailed(AdError error)
    {
        Debug.LogError($"❌ Rewarded Ad Show Failed: {error}");
        isAdShowing = false;
        isRewardedAdLoaded = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        LoadAdMobRewardedAd();
    }

    private void OnRewardedClicked()
    {
        Debug.Log("👆 Rewarded Ad Clicked");
    }

    private void OnRewardedImpression()
    {
        Debug.Log("👁️ Rewarded Ad Impression Recorded");
    }

    private IEnumerator RetryRewardedLoad(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (!isRewardedAdLoaded && isInitialized && !isLoadingRewardedAd)
        {
            Debug.Log("🔄 Retrying Rewarded Ad Load...");
            LoadAdMobRewardedAd();
        }
    }

    // =========================================================
    // LOAD INTERSTITIAL AD
    // =========================================================

    private void LoadAdMobInterstitialAd()
    {
        if (admobInterstitialAd != null)
        {
            admobInterstitialAd.Destroy();
            admobInterstitialAd = null;
        }

        isInterstitialAdLoaded = false;

        string adUnitId = GetInterstitialAdUnitId();
        Debug.Log($"🔄 Loading Interstitial Ad: {adUnitId}");
        
        AdRequest request = new AdRequest();
        InterstitialAd.Load(adUnitId, request, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError($"❌ AdMob Interstitial Load Failed: {error}");
                isInterstitialAdLoaded = false;
                StartCoroutine(RetryInterstitialLoad(5f));
                return;
            }
            if (ad == null)
            {
                Debug.LogError("❌ AdMob Interstitial Ad is null");
                isInterstitialAdLoaded = false;
                return;
            }

            admobInterstitialAd = ad;
            isInterstitialAdLoaded = true;
            Debug.Log($"✅ AdMob Interstitial Ad Loaded: {adUnitId}");

            admobInterstitialAd.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("📱 Interstitial Ad Opened");
                isAdShowing = true;
            };

            admobInterstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("📱 Interstitial Ad Closed");
                isAdShowing = false;
                isInterstitialAdLoaded = false;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                
                LoadAdMobInterstitialAd();

                if (shouldShowAdOnRestart && restartCallback != null)
                {
                    restartCallback.Invoke();
                    restartCallback = null;
                    shouldShowAdOnRestart = false;
                }
                OnAdCompleted?.Invoke();
            };

            admobInterstitialAd.OnAdFullScreenContentFailed += (err) =>
            {
                Debug.LogError($"❌ AdMob Interstitial Show Failed: {err}");
                isAdShowing = false;
                isInterstitialAdLoaded = false;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                
                LoadAdMobInterstitialAd();

                if (shouldShowAdOnRestart && restartCallback != null)
                {
                    restartCallback.Invoke();
                    restartCallback = null;
                    shouldShowAdOnRestart = false;
                }
            };

            admobInterstitialAd.OnAdClicked += () =>
            {
                Debug.Log("👆 Interstitial Ad Clicked");
            };

            admobInterstitialAd.OnAdImpressionRecorded += () =>
            {
                Debug.Log("👁️ Interstitial Ad Impression Recorded");
            };
        });
    }

    private IEnumerator RetryInterstitialLoad(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (!isInterstitialAdLoaded && isInitialized)
        {
            Debug.Log("🔄 Retrying Interstitial Ad Load...");
            LoadAdMobInterstitialAd();
        }
    }

    // =========================================================
    // LOAD BANNER AD
    // =========================================================

    public void LoadBannerAd()
    {
        if (!isInitialized) return;
        if (isLoadingBanner) return;

        isLoadingBanner = true;
        Debug.Log("Loading AdMob Banner Ad...");

        if (admobBannerView != null)
        {
            string bannerAdUnitId = GetBannerAdUnitId();
            Debug.Log($"Loading Banner with Unit ID: {bannerAdUnitId}");
            
            AdRequest request = new AdRequest();
            admobBannerView.LoadAd(request);
        }
        else
        {
            CreateAdMobAds();
        }
    }

    private IEnumerator RetryLoadBanner()
    {
        yield return new WaitForSecondsRealtime(5f);
        if (isInitialized && !isBannerLoaded && bannerShouldBeVisible)
            LoadBannerAd();
    }

    private IEnumerator BannerRefreshRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(bannerRefreshInterval);

            if (bannerShouldBeVisible && isBannerLoaded)
            {
                Debug.Log($"🔄 Refreshing Banner Ad after {bannerRefreshInterval} seconds...");
                LoadBannerAd();
            }
        }
    }

    // =========================================================
    // SHOW REWARDED AD
    // =========================================================

    public void ShowRewardedAd()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("❌ AdMob not initialized.");
            return;
        }

        if (isAdShowing)
        {
            Debug.LogWarning("⚠️ Another ad is showing. Waiting...");
            StartCoroutine(RetryShowRewardedAd());
            return;
        }

        if (isRewardClaimed)
        {
            Debug.LogWarning("⚠️ Reward already claimed! Use ResetRewardClaim() first.");
            return;
        }

        if (admobRewardedAd != null && admobRewardedAd.CanShowAd())
        {
            Debug.Log("▶️ Showing AdMob Rewarded Ad...");
            isAdShowing = true;
            
            if (isBannerShowing) HideBannerAd();
            
            admobRewardedAd.Show((reward) =>
            {
                Debug.Log($"🎁 Reward Earned: {reward.Type} - {reward.Amount}");
                isRewardClaimed = true;
                OnRewardEarned?.Invoke();
                
                StartCoroutine(AutoResetRewardClaim());
            });
            return;
        }

        Debug.LogWarning("⚠️ Rewarded Ad not ready. Loading and retrying...");
        LoadAdMobRewardedAd();
        StartCoroutine(RetryShowRewardedAd());
    }

    private IEnumerator RetryShowRewardedAd()
    {
        yield return new WaitForSeconds(2f);
        
        if (isAdShowing)
        {
            Debug.Log("🔄 Resetting stuck ad showing state...");
            isAdShowing = false;
        }
        
        if (isRewardedAdLoaded && !isRewardClaimed)
        {
            ShowRewardedAd();
        }
        else
        {
            Debug.Log("⚠️ Ad still not ready. Please try again later.");
        }
    }

    // =========================================================
    // AUTO RESET REWARD CLAIM
    // =========================================================

    private IEnumerator AutoResetRewardClaim()
    {
        yield return new WaitForSeconds(2f);
        ResetRewardClaim();
    }

    // =========================================================
    // RESET REWARD CLAIM
    // =========================================================

    public void ResetRewardClaim()
    {
        isRewardClaimed = false;
        isAdShowing = false;
        Debug.Log("🔄 Reward claim reset. Revive available again.");
        
        LoadAdMobRewardedAd();
        OnRewardReset?.Invoke();
    }

    // =========================================================
    // RESET FOR NEW GAME
    // =========================================================

    public void ResetForNewGame()
    {
        Debug.Log("🔄 Resetting AdManager for new game...");
        
        isRewardClaimed = false;
        isAdShowing = false;
        
        if (admobRewardedAd != null)
        {
            admobRewardedAd.Destroy();
            admobRewardedAd = null;
        }
        
        if (isInitialized)
        {
            LoadAdMobRewardedAd();
            LoadAdMobInterstitialAd();
            LoadBannerAd();
        }
        
        lastAdTime = 0f;
        OnRewardReset?.Invoke();
    }

    // =========================================================
    // SHOW INTERSTITIAL AD
    // =========================================================

    public void ShowInterstitialAd()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AdMob not initialized.");
            CompleteRestartWithoutAd();
            return;
        }

        if (isAdShowing)
        {
            Debug.Log("Another ad is showing. Waiting...");
            CompleteRestartWithoutAd();
            return;
        }

        if (admobInterstitialAd != null && admobInterstitialAd.CanShowAd())
        {
            isAdShowing = true;
            if (isBannerShowing) HideBannerAd();
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Debug.Log("Showing AdMob Interstitial Ad...");
            admobInterstitialAd.Show();
            return;
        }

        Debug.LogWarning("Interstitial Ad not ready.");
        CompleteRestartWithoutAd();
        LoadAdMobInterstitialAd();
    }

    // =========================================================
    // SHOW/HIDE BANNER AD
    // =========================================================

    public void ShowBannerAd()
    {
        if (!bannerShouldBeVisible || isBannerShowing) return;

        if (admobBannerView != null)
        {
            admobBannerView.Show();
            isBannerShowing = true;
            Debug.Log("✅ Showing AdMob Banner Ad");

            if (bannerRefreshCoroutine == null)
            {
                bannerRefreshCoroutine = StartCoroutine(BannerRefreshRoutine());
            }
        }
        else
        {
            Debug.LogWarning("⚠️ BannerView is null! Creating new one...");
            CreateAdMobAds();
        }
    }

    public void HideBannerAd()
    {
        if (admobBannerView != null)
        {
            admobBannerView.Hide();
            isBannerShowing = false;
            Debug.Log("Banner Ad Hidden");
        }

        if (bannerRefreshCoroutine != null)
        {
            StopCoroutine(bannerRefreshCoroutine);
            bannerRefreshCoroutine = null;
        }
    }

    // =========================================================
    // SCENE MANAGEMENT
    // =========================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log($"Scene Loaded: {currentSceneName}");

        AudioListener.pause = false;
        Time.timeScale = 1f;

        if (shouldShowAdOnRestart && restartCallback != null)
        {
            restartCallback.Invoke();
            restartCallback = null;
            shouldShowAdOnRestart = false;
        }

        isAdShowing = false;

        if (isInitialized)
        {
            LoadAdMobRewardedAd();
            LoadAdMobInterstitialAd();
            LoadBannerAd();
        }

        UpdateBannerVisibilityForScene(scene.name);
    }

    private void UpdateBannerVisibilityForScene(string sceneName)
    {
        bool shouldShowBanner = false;

        if (sceneName == "Mainmenu" || sceneName == "MainMenu")
            shouldShowBanner = showBannerOnMainMenu;
        else if (sceneName == "Gameover" || sceneName == "GameOver" || sceneName == "Game Over")
            shouldShowBanner = showBannerOnGameOver;
        else if (sceneName.StartsWith("Level") || sceneName == "Level1" || sceneName == "Level_1")
            shouldShowBanner = showBannerOnLevels;
        else if (sceneName == "Youwin" || sceneName == "YouWin" || sceneName == "You Win")
            shouldShowBanner = true;

        bannerShouldBeVisible = shouldShowBanner;

        if (shouldShowBanner)
        {
            if (isBannerLoaded && !isBannerShowing)
                ShowBannerAd();
            else if (!isBannerLoaded && !isLoadingBanner)
                LoadBannerAd();
        }
        else
        {
            if (isBannerShowing)
                HideBannerAd();
        }
    }

    // =========================================================
    // RESTART SYSTEM
    // =========================================================

    public void RequestRestartWithAd(Action callback, string sceneName = "")
    {
        restartCallback = callback;

        float timeSinceLastAd = Time.unscaledTime - lastAdTime;

        if (timeSinceLastAd >= minTimeBetweenAds)
        {
            float randomValue = UnityEngine.Random.Range(0f, 1f);

            if (randomValue <= adChanceOnRestart && isInterstitialAdLoaded)
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

    // =========================================================
    // PUBLIC UTILITY METHODS
    // =========================================================

    public bool IsInterstitialReady()
    {
        return isInitialized && isInterstitialAdLoaded && !isAdShowing;
    }

    public bool IsRewardedReady()
    {
        return isInitialized && isRewardedAdLoaded && !isAdShowing && !isRewardClaimed && !isLoadingRewardedAd;
    }

    public bool IsRewardAvailable()
    {
        return !isRewardClaimed;
    }

    public void ForceLoadAllAds()
    {
        if (isInitialized)
        {
            LoadAdMobRewardedAd();
            LoadAdMobInterstitialAd();
            LoadBannerAd();
        }
    }

    public void OnPlayerDied()
    {
        ResetRewardClaim();
    }

    public void ForceResetAdState()
    {
        isAdShowing = false;
        isRewardClaimed = false;
        Debug.Log("🔄 Force reset ad state!");
        LoadAdMobRewardedAd();
    }
}