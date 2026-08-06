using UnityEngine;
using System.Collections;

//CHANGES FOR SAHIL 
//INSIDE START FUNCTION
//INSIDE IENUMERATOR METHOD RIGHT ROUTINE

public class TutorialUIManager : MonoBehaviour
{
    private const string TutorialKey = "TutorialCompleted";

    public static TutorialUIManager Instance;
    public enum TutorialState
    {
        Left,
        Right,
        Completed
    }

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject screenPartition;

    [SerializeField] private CanvasGroup leftTutorial;
    [SerializeField] private CanvasGroup rightTutorial;

    [Header("Important References")]
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private ScoreManager  scoreManager;

    [Header("Testing")]
    [SerializeField] private bool isTesting = false;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.3f;

    private TutorialState currentState;

    public bool IsTutorialActive => currentState != TutorialState.Completed;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        //for testing purpose
        if (isTesting)
        {
            PlayerPrefs.DeleteKey(TutorialKey);
            PlayerPrefs.Save();

            Debug.Log("Tutorial progress has been reset for testing.");
        }


        bool alreadyCompleted = PlayerPrefs.GetInt(TutorialKey, 0) == 1;

        //if already tutorial level completed then directly play game without Displaying Tutorial UI
        if (alreadyCompleted)
        {
            //Destroy Tutorial Display Panel if it is not null
            if (tutorialPanel != null)
                Destroy(tutorialPanel);

            //CHANGES FOR SAHIL
            //JUST ON YOUR OBSTACLE SPAWNING SCRIPT
            if (obstacleSpawner != null)
                obstacleSpawner.enabled = true;

            //CHANGES FOR SAHIL
            //JUST ON YOUR SCORING SCRIPT
            if (scoreManager != null)
                scoreManager.ResumeScoring();

            //change state to Tutorial completed
            currentState = TutorialState.Completed;

            //don't execute below code if already complete tutorial UI is displayed
            return;
        }

        //if complete tutorial UI is not displayed yet

        //CHANGES FOR SAHIL
        //JUST OFF YOUR OBSTACLE SPAWNING SCRIPT
        if (obstacleSpawner != null)
            obstacleSpawner.enabled = false;

        //CHANGES FOR SAHIL
        //JUST OFF YOUR SCORING SCRIPT
        if (scoreManager != null)
            scoreManager.PauseScoring();

        //change state to TutorialState.Left
        currentState = TutorialState.Left;

        //Set Active CanvasGroup of Left Tutorial UI Images & also set alpha as 1 
        if (leftTutorial != null)
        {
            leftTutorial.gameObject.SetActive(true);
            leftTutorial.alpha = 1;
        }

        //Deactivate CanvasGroup of Right Tutorial UI Images
        if (rightTutorial != null)
        {
            rightTutorial.gameObject.SetActive(false);
        }
    }

    //Call this method from Player Controller script , when player holds on left side screen
    public void OnLeftTutorialCompleted()
    {
        //if currentState not equal to TutorialState.left then don't execute below 
        if (currentState != TutorialState.Left)
            return;

        //Calling Coroutine Method LeftRoutine
        StartCoroutine(LeftRoutine());
    }

    IEnumerator LeftRoutine()
    {
        //Call coroutine method Of FadeOut for leftTutorial Images
        yield return StartCoroutine(FadeOut(leftTutorial));
        //wait for FadeOut Coroutine Method
        //if FadeOut Coroutine Method executed completely then only go for below code

        //Destroy Left Tutorial 
        Destroy(leftTutorial.gameObject);

        //now Change State to Tutorial State.right
        currentState = TutorialState.Right;

        //activate CanvasGroup of Right Tutorial UI Images & also set alpha as 1
        rightTutorial.gameObject.SetActive(true);
        rightTutorial.alpha = 1;
    }


    //Call this method from Player Controller script , when player holds on right side screen
    public void OnRightTutorialCompleted()
    {
        //if currentState not equal to TutorialState.right then don't execute below 
        if (currentState != TutorialState.Right)
            return;

        //Calling Coroutine Method RightRoutine
        StartCoroutine(RightRoutine());
    }

    IEnumerator RightRoutine()
    {
        //Call coroutine method Of FadeOut for leftTutorial Images
        yield return StartCoroutine(FadeOut(rightTutorial));
        //wait for FadeOut Coroutine Method
        //if FadeOut Coroutine Method executed completely then only go for below code

        //Destroy right Tutorial 
        Destroy(rightTutorial.gameObject);

        //Destroy ScreenPartion image
        if (screenPartition != null)
            Destroy(screenPartition);

        //CHANGES FOR SAHIL
        //JUST ON YOUR OBSTACLE SPAWNING SCRIPT
        if (obstacleSpawner != null)
            obstacleSpawner.enabled = true;

        //CHANGES FOR SAHIL
        //JUST ON YOUR SCORING SCRIPT
        if (scoreManager != null)
            scoreManager.ResumeScoring();

        //Save data in Playerprefs
        PlayerPrefs.SetInt(TutorialKey, 1);
        PlayerPrefs.Save();

        //change current state to Tutorial State completed
        currentState = TutorialState.Completed;

        //Destroy overall Tutorial Panel
        Destroy(tutorialPanel);
    }



    IEnumerator FadeOut(CanvasGroup group)
    {
        if (group == null)
            yield break;

        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }

        group.alpha = 0;
    }
}
