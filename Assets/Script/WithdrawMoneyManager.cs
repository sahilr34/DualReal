using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WithdrawMoneyManager : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField mobileNumberInputField;

    [Header("Withdraw Button")]
    [SerializeField] private Button withdrawButton;

    [Header("Google Apps Script URL")]
    [SerializeField]
    private string scriptURL =
        "https://script.google.com/macros/s/AKfycbx3LyY3wmehQBK46H9wmtlqdNBPiAKpx9Mj8v-_7NyhX_JMP0cGayMUDM_G_ld9fp-Z/exec";

    [Header("Notification UI")]
    [SerializeField] private GameObject popupUI;
    private CanvasGroup notificationCanvasGroup;
    private TMP_Text notificationText;

    [Header("Notification Animation")]
    private float fadeInDuration = 0.25f;
    private float displayDuration = 2.5f;
    private float fadeOutDuration = 0.5f;


    private bool isSubmitting = false;

    private const int WITHDRAW_MULTIPLE = 100;

    private Coroutine notificationCoroutine;

    private void Awake()
    {
        notificationCanvasGroup = popupUI.GetComponent<CanvasGroup>();
        notificationText= popupUI.GetComponentInChildren<TMP_Text>(); ;
    }

    private void Start()
    {
        UpdateWithdrawButtonState();

        nameInputField.onValueChanged.AddListener(OnInputChanged);
        mobileNumberInputField.onValueChanged.AddListener(OnInputChanged);

        if (notificationCanvasGroup != null)
        {
            notificationCanvasGroup.alpha = 0f;
            notificationCanvasGroup.interactable = false;
            notificationCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnInputChanged(string value)
    {
        UpdateWithdrawButtonState();
    }

    // =========================================================
    // WITHDRAW BUTTON STATE
    // =========================================================

    private void UpdateWithdrawButtonState()
    {
        if (withdrawButton == null)
            return;

        bool validName = !string.IsNullOrWhiteSpace(nameInputField.text);
        bool validMobile = IsValidMobileNumber();
        bool hasEnoughCoins = HasWithdrawableCoins();

        withdrawButton.interactable =
            validName &&
            validMobile &&
             hasEnoughCoins &&
            !isSubmitting;


    }

    // =========================================================
    // MOBILE NUMBER VALIDATION
    // =========================================================

    private bool IsValidMobileNumber()
    {
        string mobile = mobileNumberInputField.text.Trim();

        if (mobile.Length != 10)
            return false;

        for (int i = 0; i < mobile.Length; i++)
        {
            if (!char.IsDigit(mobile[i]))
                return false;
        }

        return true;
    }

    private bool HasWithdrawableCoins()
    {
        if (CoinManager.Instance == null)
            return false;

        int availableBalance = CoinManager.Instance.CurrentCoins;

        // Must have at least 50 coins.
        return availableBalance >= WITHDRAW_MULTIPLE;
    }

    private int GetClaimableCoins(int availableCoins)
    {
        /*
         * Examples:
         *
         * 63  -> 50
         * 100 -> 100
         * 150 -> 150
         * 499 -> 450
         * 500 -> 500
         * 550 -> 550
         *
         * Anything below 50 -> 0
         */

        if (availableCoins < WITHDRAW_MULTIPLE)
            return 0;

        return  (availableCoins / WITHDRAW_MULTIPLE) * WITHDRAW_MULTIPLE;
    }

    // =========================================================
    // WITHDRAW
    // =========================================================

    public void WithdrawMoney()
    {
        if (isSubmitting)
            return;

        string name = nameInputField.text.Trim();
        string mobile = mobileNumberInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("Please enter your name.");
            return;
        }

        if (!IsValidMobileNumber())
        {
            Debug.LogWarning("Please enter a valid 10-digit mobile number.");
            return;
        }

        if (CoinManager.Instance == null)
        {
            Debug.LogError("CoinManager.Instance is NULL!");
            return;
        }

        int availableBalance = CoinManager.Instance.CurrentCoins;

        if (availableBalance <= 0)
        {
            Debug.LogWarning("You don't have enough coins to withdraw.");
            return;
        }

        int claimableCoins =  GetClaimableCoins(availableBalance);

        if (claimableCoins < WITHDRAW_MULTIPLE)
        {
            Debug.LogWarning(
                $"You need at least {WITHDRAW_MULTIPLE} coins to withdraw."
            );

            return;
        }

        int remainingCoins =
            availableBalance - claimableCoins;



        Debug.Log(
            $"Withdrawal requested | " +
            $"Name: {name} | " +
            $"Mobile: {mobile} | " +
            $"Current Balance: {availableBalance} | " +
            $"Claimable: {claimableCoins} | " +
            $"Remaining: {remainingCoins}"
        );

        StartCoroutine(SendWithdrawalData(name, mobile, claimableCoins));
    }

    // =========================================================
    // SEND DATA TO GOOGLE SHEETS
    // =========================================================

    private IEnumerator SendWithdrawalData(string name,string mobile,int claimableCoins)
    {
        isSubmitting = true;
        UpdateWithdrawButtonState();

        Debug.Log(
          $"Sending {claimableCoins} coins to Google Sheets..."
      );


        Debug.Log("Sending withdrawal data to Google Sheets...");

        WWWForm form = new WWWForm();

        string currentDate = DateTime.Now.ToString("dd-MM-yyyy");
        string currentTime = DateTime.Now.ToString("HH:mm:ss");

        // Data sent to Google Apps Script
        form.AddField("fullname", name);
        form.AddField("mobile", mobile);
        form.AddField("coins", claimableCoins.ToString());
        form.AddField("date", currentDate);
        form.AddField("time", currentTime);

        UnityWebRequest www = UnityWebRequest.Post(scriptURL, form);

        yield return www.SendWebRequest();

        Debug.Log("Response Code: " + www.responseCode);

        if (www.downloadHandler != null)
        {
            Debug.Log("Server Response: " + www.downloadHandler.text);
        }

        // =====================================================
        // SUCCESS
        // =====================================================

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Withdrawal data saved successfully!");

            // Coins deduct only after successful Google Sheet request
            bool spentSuccessfully = CoinManager.Instance.SpendCoins(claimableCoins);

            if (spentSuccessfully)
            {
                int remainingCoins = CoinManager.Instance.CurrentCoins;
                Debug.Log($"Withdrawal successful! " +
                    $"Withdrawn: {claimableCoins} | " +
                    $"Remaining: {remainingCoins}"
                );


                ShowNotification("Money will be credited in your bank within 24 hrs");
                nameInputField.text = "";
                mobileNumberInputField.text = "";
            }
            else
            {
                Debug.LogError(
                    "Google Sheets succeeded, but coins could not be spent."
                );
            }
        }

        // =====================================================
        // FAILED
        // =====================================================

        else
        {
            Debug.LogError(
                "Failed to send withdrawal data: " + www.error
            );

            Debug.LogError(
                "Withdrawal was NOT processed. Coins were NOT spent."
            );
        }

        isSubmitting = false;
        UpdateWithdrawButtonState();

        www.Dispose();
    }

    private void ShowNotification(string message)
    {
        if (popupUI == null || notificationCanvasGroup == null ||notificationText == null)
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

        yield return StartCoroutine(FadeCanvasGroup(0f,1f,fadeInDuration));


        // -----------------------------------------------------
        // DISPLAY
        // -----------------------------------------------------

        yield return new WaitForSeconds(displayDuration);


        // -----------------------------------------------------
        // FADE OUT
        // -----------------------------------------------------

        yield return StartCoroutine(FadeCanvasGroup(1f,0f,fadeOutDuration)
        );


        popupUI.SetActive(false);
        notificationCoroutine = null;
    }


    // =========================================================
    // FADE CANVAS GROUP
    // =========================================================

    private IEnumerator FadeCanvasGroup(float startAlpha,float targetAlpha,float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;


            // Smooth animation.
            progress = Mathf.SmoothStep(0f,1f,progress);


            notificationCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }
        // Ensure exact final value.
        notificationCanvasGroup.alpha = targetAlpha;
    }
}