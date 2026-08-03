using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Settings")]
    public GameObject[] obstaclePrefabs; // Multiple prefabs
    public float interval = 1.5f;
    public float fallSpeed = 3f; // Speed of falling obstacles

    [Tooltip("Set fixed Z rotation for each obstacle in the same order as prefabs")]
    public float[] fixedZRotations; // Different rotations for each prefab

    [Header("Spawn Range")]
    public float leftLimit = -3f;  // Left X position
    public float rightLimit = 3f;  // Right X position
    public float spawnY = 6f;      // Top Y position

    void Start()
    {
        InvokeRepeating(nameof(Spawn), 1f, interval);
    }

    void Spawn()
    {
        if (obstaclePrefabs.Length == 0) return;

        // Pick random index
        int index = Random.Range(0, obstaclePrefabs.Length);

        // Select prefab & matching rotation
        GameObject prefab = obstaclePrefabs[index];
        float zRotation = (index < fixedZRotations.Length) ? fixedZRotations[index] : 0f;

        // Random X position
        float randomX = Random.Range(leftLimit, rightLimit);

        // Spawn with specific rotation
        Quaternion rotation = Quaternion.Euler(0, 0, zRotation);
        GameObject newObstacle = Instantiate(prefab, new Vector2(randomX, spawnY), rotation);

        // Tag ensure
        newObstacle.tag = "Obstacle"; // <-- Tag set karo

        // Rigidbody2D settings
        Rigidbody2D rb = newObstacle.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = newObstacle.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.down * fallSpeed;
        rb.freezeRotation = true;
        rb.isKinematic = false; // Important for trigger detection

        // Collider settings
        Collider2D col = newObstacle.GetComponent<Collider2D>();
        if (col == null)
            col = newObstacle.AddComponent<BoxCollider2D>();

        col.isTrigger = false; // Trigger off for obstacle
    }
}
