using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class DuetPCController : MonoBehaviour
{
    public float rotateSpeed = 180f;

    private float dir = 0f;
    private bool leftTutorialDone;
    private bool rightTutorialDone;

    private void Update()
    {

        // Mobile Controls as well as PC Controls
        if (Pointer.current == null)
            return;

        // Rotate ONLY while pressing/touching
        if (Pointer.current.press.isPressed)
        {
            Vector2 pointerPos = Pointer.current.position.ReadValue();

            if (pointerPos.x < Screen.width * 0.5f)
            {
                dir = -1f;

                if (!leftTutorialDone &&
                    TutorialUIManager.Instance != null &&
                    TutorialUIManager.Instance.IsTutorialActive)
                {
                    leftTutorialDone = true;
                    TutorialUIManager.Instance.OnLeftTutorialCompleted();
                }
            }
            else
            {
                dir = 1f;

                if (!rightTutorialDone &&
                    TutorialUIManager.Instance != null &&
                    TutorialUIManager.Instance.IsTutorialActive)
                {
                    rightTutorialDone = true;
                    TutorialUIManager.Instance.OnRightTutorialCompleted();
                }
            }
        }

        else
            dir = 0f;

        // Rotate only when dir != 0
        if (dir != 0f)
        {
            transform.Rotate(0f, 0f, dir * rotateSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle"))
        {
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();

            if (scoreManager != null)
            {
                scoreManager.StopAndSaveScore();
            }

            if (!GameState.reviveUsed)
            {
                GameState.lastGameScene = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene("Revive");
            }
            else
            {
                SceneManager.LoadScene("GameOver");
            }
        }
    }
}