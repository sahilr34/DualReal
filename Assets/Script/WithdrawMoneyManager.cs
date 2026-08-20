using System.Collections;
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
        "https://script.google.com/macros/s/AKfycbxRknu2JN_IXR8jBSERun33hUkDOGwQMuj59qaIeTLPwTY-Z53_hnAbay4M2NUxVWTHaQ/exec";

    private bool isSubmitting = false;

    private void Start()
    {
        // Make sure the button starts with the correct state
        UpdateWithdrawButtonState();

        // Listen for changes in the input fields
        nameInputField.onValueChanged.AddListener(OnInputChanged);
        mobileNumberInputField.onValueChanged.AddListener(OnInputChanged);
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

        withdrawButton.interactable =
            validName &&
            validMobile &&
            !isSubmitting;
    }

    // =========================================================
    // MOBILE NUMBER VALIDATION
    // =========================================================

    private bool IsValidMobileNumber()
    {
        string mobile = mobileNumberInputField.text.Trim();

        // Must contain exactly 10 digits
        if (mobile.Length != 10)
            return false;

        for (int i = 0; i < mobile.Length; i++)
        {
            if (!char.IsDigit(mobile[i]))
                return false;
        }

        return true;
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

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("Please enter your name.");
            return;
        }

        // Validate mobile number
        if (!IsValidMobileNumber())
        {
            Debug.LogWarning("Please enter a valid 10-digit mobile number.");
            return;
        }

        // Get current coins
        if (CoinManager.Instance == null)
        {
            Debug.LogError("CoinManager.Instance is NULL!");
            return;
        }

        int availableBalance = CoinManager.Instance.CurrentCoins;

        // Prevent withdrawal if balance is zero
        if (availableBalance <= 0)
        {
            Debug.LogWarning("You don't have enough coins to withdraw.");
            return;
        }

        Debug.Log(
            $"Withdrawal requested | " +
            $"Name: {name} | " +
            $"Mobile: {mobile} | " +
            $"Balance: {availableBalance}"
        );

        StartCoroutine(SendWithdrawalData(name, mobile, availableBalance));
    }

    // =========================================================
    // SEND DATA TO GOOGLE SHEETS
    // =========================================================

    private IEnumerator SendWithdrawalData(
        string name,
        string mobile,
        int availableBalance)
    {
        isSubmitting = true;
        UpdateWithdrawButtonState();

        Debug.Log("Sending withdrawal data to Google Sheets...");

        WWWForm form = new WWWForm();

        // Data sent to Google Apps Script
        form.AddField("name", name);
        form.AddField("mobile", mobile);
        form.AddField("availableBalance", availableBalance.ToString());

        UnityWebRequest www =
            UnityWebRequest.Post(scriptURL, form);

        yield return www.SendWebRequest();

        Debug.Log("Response Code: " + www.responseCode);

        if (www.downloadHandler != null)
        {
            Debug.Log(
                "Server Response: " +
                www.downloadHandler.text
            );
        }

        // =====================================================
        // SUCCESS
        // =====================================================

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Withdrawal data saved successfully!");

            // Spend coins ONLY after successful Google Sheets request
            bool spentSuccessfully =
                CoinManager.Instance.SpendCoins(availableBalance);

            if (spentSuccessfully)
            {
                Debug.Log(
                    $"Withdrawal successful. " +
                    $"Spent {availableBalance} coins."
                );

                // Clear input fields
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
                "Failed to send withdrawal data: " +
                www.error
            );

            Debug.LogError(
                "Withdrawal was NOT processed. Coins were NOT spent."
            );
        }

        isSubmitting = false;
        UpdateWithdrawButtonState();

        www.Dispose();
    }
}