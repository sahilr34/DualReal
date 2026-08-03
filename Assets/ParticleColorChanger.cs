using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleColorChanger : MonoBehaviour
{
    public Color color1 = Color.red;
    public Color color2 = new Color(0.4f, 0.8f, 1f); // Sky Blue

    [Tooltip("Color change speed")]
    public float speed = 2f;

    private ParticleSystem ps;
    private ParticleSystem.MainModule main;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        main = ps.main;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        main.startColor = Color.Lerp(color1, color2, t);
    }
}