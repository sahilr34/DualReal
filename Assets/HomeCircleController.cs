using UnityEngine;

public class HomeCircleController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotateSpeed = 180f;
    public bool rotateClockwise = true;

    private void Update()
    {
        float dir = rotateClockwise ? 1f : -1f;
        transform.Rotate(0f, 0f, dir * rotateSpeed * Time.deltaTime);
    }
}