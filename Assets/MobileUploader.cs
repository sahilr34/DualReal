using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class MobileUploader : MonoBehaviour
{
    public TMP_InputField mobileInput;

    [Header("Google Apps Script URL")]
    public string scriptURL = "https://script.google.com/macros/s/AKfycbxRknu2JN_IXR8jBSERuN33hUkDOGwQMuj59qaIeTLPwTY-Z53_hnAbay4M2NUxVWTHaQ/exec";

    [Header("Notification UI")]
    [SerializeField] private GameObject popupUI;
    private CanvasGroup notificationCanvasGroup;
    private TMP_Text notificationText;

    private Coroutine notificationCoroutine;
    private float fadeInDuration = 0.25f;
    private float displayDuration = 2.5f;
    private float fadeOutDuration = 0.5f;
    void Start()
    {
        Debug.Log("MobileUploader Started");
        notificationCanvasGroup = popupUI.GetComponent<CanvasGroup>();
        notificationText = popupUI.GetComponentInChildren<TMP_Text>(); ;

        if (notificationCanvasGroup != null)
        {
            notificationCanvasGroup.alpha = 0f;
            notificationCanvasGroup.interactable = false;
            notificationCanvasGroup.blocksRaycasts = false;
        }
    }

    public void SubmitMobile()
    {
        Debug.Log("Submit Button Clicked");
        Debug.Log("Mobile Number: " + mobileInput.text);

        if (mobileInput.text.Length != 10)
        {
            Debug.Log("Please enter a valid 10-digit mobile number.");
            return;
        }

        StartCoroutine(SendMobile());
    }

    IEnumerator SendMobile()
    {
        Debug.Log("Sending data to Google Sheet...");

        WWWForm form = new WWWForm();
        form.AddField("mobile", mobileInput.text);

        UnityWebRequest www = UnityWebRequest.Post(scriptURL, form);

        yield return www.SendWebRequest();

        Debug.Log("Response Code : " + www.responseCode);

        if (www.downloadHandler != null)
            Debug.Log("Server Response : " + www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Saved Successfully!");
            mobileInput.text = "";

            ShowNotification("Money will be credited in your bank within 24 hrs");
        }
        else
        {
            Debug.LogError("Error : " + www.error);
        }
    }

    private void ShowNotification(string message)
    {
        if (popupUI == null || notificationCanvasGroup == null || notificationText == null)
        {
            Debug.LogWarning("Notification UI is not completely assigned.");
            return;
        }


        // Set notification text.
        notificationText.text = message;

        // Stop previous notification animation.
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }


        // Start new notification animation.
        notificationCoroutine = StartCoroutine(NotificationAnimation());
    }


    // =========================================================
    // NOTIFICATION ANIMATION
    // =========================================================

    private IEnumerator NotificationAnimation()
    {
        popupUI.SetActive(true);

        // Make sure we start from invisible.
        notificationCanvasGroup.alpha = 0f;

        // -----------------------------------------------------
        // FADE IN
        // -----------------------------------------------------

        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));


        // -----------------------------------------------------
        // DISPLAY
        // -----------------------------------------------------

        yield return new WaitForSeconds(displayDuration);


        // -----------------------------------------------------
        // FADE OUT
        // -----------------------------------------------------

        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration)
        );


        popupUI.SetActive(false);
        notificationCoroutine = null;
    }


    // =========================================================
    // FADE CANVAS GROUP
    // =========================================================

    private IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;


            // Smooth animation.
            progress = Mathf.SmoothStep(0f, 1f, progress);


            notificationCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }
        // Ensure exact final value.
        notificationCanvasGroup.alpha = targetAlpha;
    }
}