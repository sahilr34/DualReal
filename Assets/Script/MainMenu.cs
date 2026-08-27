using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private AudioSource audioSource;

    [SerializeField] private AudioClip mainMenuBgm;
    [SerializeField] private GameObject slashImage;
    [Tooltip ("In settings Panel")]
    [SerializeField] private Slider bgmSlider;
    private float currentVolume;
    private float previousVolume;

    private bool isBgmOn = true;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        currentVolume = 1f;
        previousVolume = 1f;

        // Set up BGM
        audioSource.clip = mainMenuBgm;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        bgmSlider.value = 1f;

        // Set BGM volume according to slider
        audioSource.volume = currentVolume;


        // Start BGM
        audioSource.Play();

        // BGM is ON, so activate slash image
        isBgmOn = true;
        slashImage.SetActive(true);

        bgmSlider.onValueChanged.AddListener(ChangeBGMVolume);
    }

    public void ChangeBGMVolume(float volume)
    {
        currentVolume = volume;

        // Store the last non-zero slider value
        if (volume > 0f)
        {
            previousVolume = volume;
        }

        // Slider is at 0
        if (volume <= 0f)
        {
            currentVolume = 0f;

            if (isBgmOn)
            {
                isBgmOn = false;

                audioSource.Pause();

                slashImage.SetActive(false);
            }

            return;
        }

       
        audioSource.volume = volume;

        // If BGM was OFF, turn it ON automatically
        if (!isBgmOn)
        {
            isBgmOn = true;

            audioSource.UnPause();

            slashImage.SetActive(true);
        }
    }



    public void StartGame()
    {
        SceneManager.LoadScene("Level"); // Replace with your game scene name
    }

    // Called when Level button is pressed
    public void LoadLevelSelect()
    {
        SceneManager.LoadScene("Level"); // Replace with your level select scene name
    }

   

    // Called when Exit button is pressed
    public void ExitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit(); // Only works in build
    }

    public void SounBGM()
    {
       isBgmOn = !isBgmOn;

        if (isBgmOn)
        {
            // Restore previous volume
            if (currentVolume <= 0f)
            {
                currentVolume = previousVolume;
            }

            // Update slider
            bgmSlider.value = currentVolume;

            // Set volume
            audioSource.volume = currentVolume;

            // Resume BGM
            audioSource.UnPause();

            // Activate slash image
            slashImage.SetActive(true);
        }
        else
        {
            // Save current volume before turning OFF
            if (bgmSlider.value > 0f)
            {
                previousVolume = currentVolume;
            }

            // Set slider to 0
            bgmSlider.value = 0f;

            // Pause BGM
            audioSource.Pause();

            // Deactivate slash image
            slashImage.SetActive(false);
        }
    }
}
