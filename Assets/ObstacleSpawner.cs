using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Settings")]
    public GameObject[] obstaclePrefabs;

    public float interval = 1.5f;
    public float fallSpeed = 3f;

    [Tooltip("Set fixed Z rotation for each obstacle in the same order as prefabs")]
    public float[] fixedZRotations;

    [Header("Spawn Range")]
    public float leftLimit = -3f;
    public float rightLimit = 3f;
    public float spawnY = 6f;

    private void OnEnable()
    {
        // Make sure no old Invoke is running
        CancelInvoke(nameof(Spawn));

        // Start spawning whenever this component is enabled
        InvokeRepeating(
            nameof(Spawn),
            1f,
            interval
        );

        Debug.Log("ObstacleSpawner ENABLED - Spawning started.");
    }

    private void OnDisable()
    {
        // Stop spawning whenever this component is disabled
        CancelInvoke(nameof(Spawn));

        Debug.Log("ObstacleSpawner DISABLED - Spawning stopped.");
    }

    private void Spawn()
    {
        if (obstaclePrefabs == null ||
            obstaclePrefabs.Length == 0)
        {
            Debug.LogWarning(
                "ObstacleSpawner: No obstacle prefabs assigned."
            );

            return;
        }

        int index = Random.Range(
            0,
            obstaclePrefabs.Length
        );

        GameObject prefab = obstaclePrefabs[index];

        float zRotation =
            (fixedZRotations != null &&
             index < fixedZRotations.Length)
                ? fixedZRotations[index]
                : 0f;

        float randomX = Random.Range(
            leftLimit,
            rightLimit
        );

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                0f,
                zRotation
            );

        GameObject newObstacle =
            Instantiate(
                prefab,
                new Vector2(
                    randomX,
                    spawnY
                ),
                rotation
            );

        newObstacle.tag = "Obstacle";

        Rigidbody2D rb =
            newObstacle.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = newObstacle.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = 0f;

        rb.linearVelocity =
            Vector2.down * fallSpeed;

        rb.freezeRotation = true;
        rb.isKinematic = false;

        Collider2D col =
            newObstacle.GetComponent<Collider2D>();

        if (col == null)
        {
            col = newObstacle.AddComponent<BoxCollider2D>();
        }

        col.isTrigger = false;
    }
}