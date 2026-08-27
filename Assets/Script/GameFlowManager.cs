using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private ReviveManager reviveManager;
    [SerializeField] private GameOverUI gameOverUI;

    [Header("Audio")]
    [SerializeField] private AudioSource gameBGM;
    private bool gameEnded = false;

    private void Awake()
    {
        // Singleton protection
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Always start gameplay normally
        Time.timeScale = 1f;

        GameSessionState.currentGameScene =
            SceneManager.GetActiveScene().name;
    }

    private void Start()
    {
        // These references MUST come from Inspector.
        if (reviveManager != null)
            reviveManager.HidePanel();

        if (gameOverUI != null)
            gameOverUI.HidePanel();
    }

    private void OnDestroy()
    {
        // VERY IMPORTANT
        // Never leave Instance pointing to a destroyed object.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayerHit()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        Debug.Log("Player hit obstacle.");

        PauseGameBGM();


        Time.timeScale = 0f;

        if (!GameSessionState.reviveUsed)
        {
            if (reviveManager != null)
            {
                reviveManager.ShowPanel();
            }
            else
            {
                Debug.LogError(
                    "GameFlowManager: ReviveManager reference is NULL!"
                );

                ShowGameOver();
            }
        }
        else
        {
            ShowGameOver();
        }
    }

    public void RevivePlayer()
    {
        if (!gameEnded)
            return;

        Debug.Log("Player revived.");

        GameSessionState.reviveUsed = true;

        if (reviveManager != null)
            reviveManager.HidePanel();

        Time.timeScale = 1f;

        gameEnded = false;

        ResumeGameBGM();
    }

    public void ShowGameOver()
    {
        Debug.Log("Showing Game Over.");

        Time.timeScale = 0f;

        if (reviveManager != null)
            reviveManager.HidePanel();

        if (gameOverUI != null)
        {
            gameOverUI.ShowPanel();
        }
        else
        {
            Debug.LogError(
                "GameFlowManager: GameOverUI reference is NULL!"
            );
        }
    }

    private void PauseGameBGM()
    {
        if (gameBGM != null && gameBGM.isPlaying)
        {
            gameBGM.Pause();
            Debug.Log("Game BGM PAUSED.");
        }
    }

    private void ResumeGameBGM()
    {
        if (gameBGM != null)
        {
            gameBGM.UnPause();
            Debug.Log("Game BGM RESUMED.");
        }
    }

    private void RestartGameBGM()
    {
        if (gameBGM != null)
        {
            gameBGM.Stop();
            gameBGM.time = 0f;
            gameBGM.Play();

            Debug.Log("Game BGM RESTARTED.");
        }
    }

    public void RestartCurrentScene()
    {
        Debug.Log(
            "Restarting current scene: " +
            SceneManager.GetActiveScene().name
        );

        // Stop everything before scene destruction.
        Time.timeScale = 1f;

        GameSessionState.reviveUsed = false;

        // Stop current BGM.
        // New scene's AudioSource will then start from the beginning.
        if (gameBGM != null)
        {
            gameBGM.Stop();
        }

        string currentScene =
            SceneManager.GetActiveScene().name;

        /*
         * Clear the singleton NOW.
         *
         * This prevents any callback or old object from
         * accessing the GameFlowManager that is about to
         * be destroyed.
         */
        if (Instance == this)
        {
            Instance = null;
        }

        SceneManager.LoadScene(currentScene);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        GameSessionState.reviveUsed = false;

        if (Instance == this)
        {
            Instance = null;
        }

        SceneManager.LoadScene("Mainmenu");
    }
}