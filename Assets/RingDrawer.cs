using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RingDrawer : MonoBehaviour
{
    [Header("References")]
    public Transform red;
    public Transform blue;

    [Header("Ring Settings")]
    public int segments = 360;
    public float width = 0.03f;
    public float offsetY = 0f;
    public float radiusOffset = 0f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();

        lr.useWorldSpace = false;
        lr.loop = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = new Color(1f, 1f, 1f, 0.25f);
        lr.endColor = new Color(1f, 1f, 1f, 0.25f);
    }

    void Start()
    {
        // Automatically find Red & Blue if not assigned
        if (red == null)
            red = transform.parent.Find("Red");

        if (blue == null)
            blue = transform.parent.Find("Blue");
    }

    void LateUpdate()
    {
        // Stop if references are missing
        if (red == null || blue == null)
            return;

        Vector3 center = (red.localPosition + blue.localPosition) * 0.5f;
        center.y += offsetY;

        float radius = Vector3.Distance(red.localPosition, blue.localPosition) * 0.5f + radiusOffset;

        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector3 pos = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius,
                0f
            );

            lr.SetPosition(i, pos);
        }
    }
}