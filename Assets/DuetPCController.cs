using UnityEngine;
using UnityEngine.SceneManagement;

public class DuetPCController : MonoBehaviour
{
    public float rotateSpeed = 180f;

    private float dir = 0f;
    private bool leftTutorialDone;
    private bool rightTutorialDone;

    private void Update()
    {
        // Keyboard Controls
        if (Input.GetKey(KeyCode.A))
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
            
        else if (Input.GetKey(KeyCode.D))
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
        else
            dir = 0f;

        // Mobile Controls
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began ||
                touch.phase == TouchPhase.Stationary ||
                touch.phase == TouchPhase.Moved)
            {
                if (touch.position.x < Screen.width / 2)
                {
                    dir = -1f;

                    if (!leftTutorialDone && TutorialUIManager.Instance != null &&
                        TutorialUIManager.Instance.IsTutorialActive)
                    {
                        leftTutorialDone = true;
                        TutorialUIManager.Instance.OnLeftTutorialCompleted();
                    }
                }
                else
                {
                    dir = 1f;
                    if (!rightTutorialDone && TutorialUIManager.Instance != null &&
                        TutorialUIManager.Instance.IsTutorialActive)
                    {
                        rightTutorialDone = true;
                        TutorialUIManager.Instance.OnRightTutorialCompleted();
                    }
                }
            }

            if (touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled)
            {
                dir = 0f;
            }
        }

        transform.Rotate(0f, 0f, dir * rotateSpeed * Time.deltaTime);
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