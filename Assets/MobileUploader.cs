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

    void Start()
    {
        Debug.Log("MobileUploader Started");
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
        }
        else
        {
            Debug.LogError("Error : " + www.error);
        }
    }
}