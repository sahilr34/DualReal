using UnityEngine;
using UnityEngine.SceneManagement;

public class InternetManager : MonoBehaviour
{
    public static InternetManager Instance;

    public GameObject noInternetPrefab;
    private GameObject popupInstance;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreatePopup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        CheckInternet();
    }

    void CreatePopup()
    {
        popupInstance = Instantiate(noInternetPrefab);
        popupInstance.SetActive(false);
        DontDestroyOnLoad(popupInstance);
    }

    void CheckInternet()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowPopup();
        }
        else
        {
            HidePopup();
        }
    }

    void ShowPopup()
    {
        if (!popupInstance.activeSelf)
        {
            popupInstance.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }
    }

    void HidePopup()
    {
        if (popupInstance.activeSelf)
        {
            popupInstance.SetActive(false);
            Time.timeScale = 1f; // Resume game
        }
    }

    public void Retry()
    {
        CheckInternet();
    }
}
