using UnityEngine;
using UnityEngine.Advertisements;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class AdManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Ad Settings")]
    public string androidGameId = "5982831";
    public string iosGameId = "5982830";
    public string androidInterstitialAdUnitId = "Interstitial_Android";
    public string iosInterstitialAdUnitId = "Interstitial_iOS";
    public string androidRewardedAdUnitId = "Rewarded_Android";
    public string iosRewardedAdUnitId = "Rewarded_iOS";
    public string androidBannerAdUnitId = "Banner_Android";
    public string iosBannerAdUnitId = "Banner_iOS";
    public bool testMode = true;

    [Header("Ad Frequency")]
    [Tooltip("Chance to show ad on restart (0-1)")]
    [Range(0f, 1f)]
    public float adChanceOnRestart = 0f; // Set to 0 to disable restart ads
    [Tooltip("Minimum time between ads in seconds")]
    public float minTimeBetweenAds = 60f;

    [Header("Banner Settings")]
    [Tooltip("Show banner on main menu")]
    public bool showBannerOnMainMenu = true;
    [Tooltip("Show banner on game over")]
    public bool showBannerOnGameOver = true;
    [Tooltip("Show banner on level scenes")]
    public bool showBannerOnLevels = true;
    [Tooltip("Banner position")]
    public BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;

    public static AdManager Instance;

    private bool isInitialized = false;
    private bool isInterstitialAdLoaded = false;
    private bool isRewardedAdLoaded = false;
    private bool isAdShowing = false;

    private float lastAdTime = 0f;
    private bool shouldShowAdOnRestart = false;

    private Action restartCallback;
    private string targetSceneName = "";

    // Banner related
    private bool isBannerLoaded = false;
    private bool isBannerShowing = false;
    private string currentSceneName = "";
    private Coroutine bannerRetryCoroutine;
    private bool bannerShouldBeVisible = true;
    private bool isLoadingBanner = false;

    // Events
    public System.Action OnAdCompleted;
    public System.Action OnRewardEarned;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Force ad chance to 0 (no ads on restart)
            adChanceOnRestart = 0f;

            InitializeAds();

            // Scene change ko track karein
            SceneManager.sceneLoaded += OnSceneLoaded;
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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log($"Scene Loaded: {currentSceneName}");

        // Scene load hone par audio unpause karein
        AudioListener.pause = false;

        // Restart callback execute karein (scene load ke baad)
        if (shouldShowAdOnRestart && restartCallback != null)
        {
            restartCallback.Invoke();
            restartCallback = null;
            shouldShowAdOnRestart = false;
        }

        // Scene ke hisaab se banner handle karein
        UpdateBannerVisibilityForScene(scene.name);
    }

    private void UpdateBannerVisibilityForScene(string sceneName)
    {
        // Check if banner should be shown in this scene
        bool shouldShowBanner = false;

        // Main Menu scenes
        if (sceneName == "Mainmenu" || sceneName == "MainMenu")
        {
            shouldShowBanner = showBannerOnMainMenu;
        }
        // Game Over scenes
        else if (sceneName == "Gameover" || sceneName == "GameOver" || sceneName == "Game Over")
        {
            shouldShowBanner = showBannerOnGameOver;
        }
        // Level scenes - ADDED "Level1" support
        else if (sceneName == "Level" || sceneName == "Level1" || sceneName == "Level_1" ||
                 sceneName == "Level2" || sceneName == "Level_2" || sceneName == "Level3" || sceneName == "Level_3" ||
                 sceneName.StartsWith("Level"))
        {
            shouldShowBanner = showBannerOnLevels;
        }
        // Win scenes
        else if (sceneName == "Youwin" || sceneName == "YouWin" || sceneName == "You Win")
        {
            shouldShowBanner = true;
        }

        bannerShouldBeVisible = shouldShowBanner;

        if (shouldShowBanner)
        {
            // Banner should be visible - show it
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
            // Banner should be hidden - hide it but don't destroy
            if (isBannerShowing)
            {
                HideBannerAd();
            }
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("✅ Unity Ads initialized successfully");
        isInitialized = true;
        LoadAllAds();

        // Load banner ad once for continuous use
        LoadBannerAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"❌ Unity Ads Initialization Failed: {error} - {message}");
        StartCoroutine(RetryInitialization(5f));
    }

    private IEnumerator RetryInitialization(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        InitializeAds();
    }

    private void InitializeAds()
    {
        if (!Advertisement.isInitialized && !isInitialized)
        {
            Debug.Log("Initializing Unity Ads...");

#if UNITY_IOS
            string gameId = iosGameId;
#elif UNITY_ANDROID
            string gameId = androidGameId;
#else
            string gameId = androidGameId;
#endif

            Advertisement.Initialize(gameId, testMode, this);
        }
        else if (Advertisement.isInitialized)
        {
            isInitialized = true;
            LoadAllAds();
        }
    }

    private void LoadAllAds()
    {
        LoadInterstitialAd();
        LoadRewardedAd();
    }

    private string GetInterstitialAdUnitId()
    {
#if UNITY_IOS
        return iosInterstitialAdUnitId;
#else
        return androidInterstitialAdUnitId;
#endif
    }

    private string GetRewardedAdUnitId()
    {
#if UNITY_IOS
        return iosRewardedAdUnitId;
#else
        return androidRewardedAdUnitId;
#endif
    }

    private string GetBannerAdUnitId()
    {
#if UNITY_IOS
        return iosBannerAdUnitId;
#else
        return androidBannerAdUnitId;
#endif
    }

    // ---------- Banner Ads - Persistent ----------
    public void LoadBannerAd()
    {
        if (!isInitialized)
        {
            Debug.Log("Ads not initialized yet.");
            return;
        }

        if (isBannerLoaded)
        {
            Debug.Log("Banner already loaded");
            if (bannerShouldBeVisible && !isBannerShowing)
            {
                ShowBannerAd();
            }
            return;
        }

        if (isLoadingBanner)
        {
            Debug.Log("Banner is already loading");
            return;
        }

        string bannerAdUnitId = GetBannerAdUnitId();
        Debug.Log($"Loading Banner Ad: {bannerAdUnitId}");

        isLoadingBanner = true;

        // Stop any existing retry coroutine
        if (bannerRetryCoroutine != null)
        {
            StopCoroutine(bannerRetryCoroutine);
            bannerRetryCoroutine = null;
        }

        // Set banner position
        Advertisement.Banner.SetPosition(bannerPosition);

        // Load banner
        Advertisement.Banner.Load(bannerAdUnitId, new BannerLoadOptions
        {
            loadCallback = () => {
                Debug.Log("✅ Banner Ad Loaded Successfully!");
                isBannerLoaded = true;
                isLoadingBanner = false;

                if (bannerShouldBeVisible)
                {
                    ShowBannerAd();
                }
            },
            errorCallback = (error) => {
                Debug.LogError($"❌ Banner Ad Failed to Load: {error}");
                isBannerLoaded = false;
                isLoadingBanner = false;

                // Retry loading after delay
                bannerRetryCoroutine = StartCoroutine(RetryLoadBanner());
            }
        });
    }

    private IEnumerator RetryLoadBanner()
    {
        yield return new WaitForSecondsRealtime(5f);
        if (!isBannerLoaded && isInitialized && bannerShouldBeVisible)
        {
            Debug.Log("Retrying to load banner...");
            LoadBannerAd();
        }
    }

    public void ShowBannerAd()
    {
        if (!isInitialized)
        {
            Debug.Log("Ads not initialized yet.");
            return;
        }

        if (!bannerShouldBeVisible)
        {
            Debug.Log("Banner should not be visible in current scene");
            return;
        }

        if (!isBannerLoaded)
        {
            Debug.Log("Banner not loaded yet. Loading now...");
            LoadBannerAd();
            return;
        }

        if (isBannerShowing)
        {
            Debug.Log("Banner already showing");
            return;
        }

        string bannerAdUnitId = GetBannerAdUnitId();
        Debug.Log($"Showing Banner Ad: {bannerAdUnitId}");

        Advertisement.Banner.Show(bannerAdUnitId, new BannerOptions
        {
            showCallback = () => {
                Debug.Log("✅ Banner Ad Shown");
                isBannerShowing = true;
            },
            hideCallback = () => {
                Debug.Log("📱 Banner Ad Hidden");
                isBannerShowing = false;
            },
            clickCallback = () => {
                Debug.Log("🔗 Banner Ad Clicked");
            }
        });
    }

    public void HideBannerAd()
    {
        if (isBannerShowing)
        {
            Debug.Log("Hiding Banner Ad");
            Advertisement.Banner.Hide();
            isBannerShowing = false;
        }
    }

    public void DestroyBannerAd()
    {
        Debug.Log("Destroying Banner Ad");
        Advertisement.Banner.Hide();
        isBannerLoaded = false;
        isBannerShowing = false;
        bannerShouldBeVisible = false;

        if (bannerRetryCoroutine != null)
        {
            StopCoroutine(bannerRetryCoroutine);
            bannerRetryCoroutine = null;
        }
    }

    // ---------- Interstitial Ads ----------
    private void LoadInterstitialAd()
    {
        if (!isInitialized) return;

        string adUnitId = GetInterstitialAdUnitId();
        Debug.Log($"Loading Interstitial Ad: {adUnitId}");
        Advertisement.Load(adUnitId, this);
    }

    public void ShowInterstitialAd()
    {
        if (!isInitialized || !isInterstitialAdLoaded || isAdShowing)
        {
            if (shouldShowAdOnRestart && restartCallback != null)
            {
                restartCallback.Invoke();
                restartCallback = null;
            }
            OnAdCompleted?.Invoke();
            shouldShowAdOnRestart = false;
            return;
        }

        string adUnitId = GetInterstitialAdUnitId();
        isAdShowing = true;

        // Hide banner temporarily when interstitial shows
        if (isBannerShowing)
        {
            Advertisement.Banner.Hide();
        }

        Time.timeScale = 0f;
        AudioListener.pause = true;
        Advertisement.Show(adUnitId, this);
    }

    // ---------- Rewarded Ads ----------
    private void LoadRewardedAd()
    {
        if (!isInitialized) return;

        string adUnitId = GetRewardedAdUnitId();
        Debug.Log($"Loading Rewarded Ad: {adUnitId}");
        Advertisement.Load(adUnitId, this);
    }

    public void ShowRewardedAd()
    {
        if (isRewardedAdLoaded && !isAdShowing)
        {
            string adUnitId = GetRewardedAdUnitId();
            isAdShowing = true;

            // Hide banner temporarily when rewarded ad shows
            if (isBannerShowing)
            {
                Advertisement.Banner.Hide();
            }

            Advertisement.Show(adUnitId, this);
        }
        else
        {
            Debug.Log("Rewarded ad not ready yet.");
            LoadRewardedAd();
        }
    }

    // ---------- Unity Ads Callbacks ----------
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log($"✅ Ad Loaded: {adUnitId}");

        if (adUnitId == GetInterstitialAdUnitId()) isInterstitialAdLoaded = true;
        if (adUnitId == GetRewardedAdUnitId()) isRewardedAdLoaded = true;
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"❌ Failed to load Ad: {adUnitId}, Error: {error} - {message}");

        if (adUnitId == GetInterstitialAdUnitId())
        {
            isInterstitialAdLoaded = false;
            StartCoroutine(RetryLoadAd(5f, true));
        }
        if (adUnitId == GetRewardedAdUnitId())
        {
            isRewardedAdLoaded = false;
            StartCoroutine(RetryLoadAd(5f, false));
        }
    }

    private IEnumerator RetryLoadAd(float delay, bool isInterstitial)
    {
        yield return new WaitForSeconds(delay);
        if (isInterstitial) LoadInterstitialAd();
        else LoadRewardedAd();
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"❌ Show Failed: {adUnitId} - {error}: {message}");
        isAdShowing = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Restore banner after ad failure
        if (bannerShouldBeVisible && isBannerLoaded)
        {
            ShowBannerAd();
        }

        if (shouldShowAdOnRestart && restartCallback != null)
        {
            restartCallback.Invoke();
            restartCallback = null;
        }
        shouldShowAdOnRestart = false;

        LoadAllAds();
    }

    public void OnUnityAdsShowStart(string adUnitId)
    {
        Debug.Log($"▶️ Ad Started: {adUnitId}");
    }

    public void OnUnityAdsShowClick(string adUnitId)
    {
        Debug.Log($"🔗 Ad Clicked: {adUnitId}");
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"✅ Ad Completed: {adUnitId}, State: {showCompletionState}");

        isAdShowing = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Restore banner after ad completes
        if (bannerShouldBeVisible && isBannerLoaded)
        {
            ShowBannerAd();
        }

        if (adUnitId == GetRewardedAdUnitId() && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("🎁 Player earned reward!");
            OnRewardEarned?.Invoke();
        }
        else if (adUnitId == GetInterstitialAdUnitId())
        {
            lastAdTime = Time.unscaledTime;
            OnAdCompleted?.Invoke();
        }

        LoadAllAds();
    }

    // Restart ke liye method
    public void RequestRestartWithAd(Action callback, string sceneName = "")
    {
        restartCallback = callback;
        targetSceneName = sceneName;

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
}