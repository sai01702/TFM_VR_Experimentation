using UnityEngine;

public class IdleMotion : MonoBehaviour
{
    public float amplitude = 0.01f;
    public float frequency = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = startPos + new Vector3(0, offset, 0);
    }
}