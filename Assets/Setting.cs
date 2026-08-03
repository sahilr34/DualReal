using UnityEngine;
using UnityEngine.SceneManagement;

public class Setting : MonoBehaviour
{
    // Back Button ke OnClick() me is function ko assign karo
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("Mainmenu");
    }
}