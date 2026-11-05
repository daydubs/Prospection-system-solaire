using UnityEngine;

public class StationRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 8f; // degrees per second
    public Vector3 rotationAxis = Vector3.up;

    [Header("Floating Levitation Settings")]
    public bool enableFloating = true;
    public float floatAmplitude = 0.08f;
    public float floatFrequency = 1.0f;

    private Vector3 initialLocalPos;

    private void Start()
    {
        initialLocalPos = transform.localPosition;
    }

    private void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

        if (enableFloating)
        {
            float offsetY = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0f, offsetY, 0f);
        }
    }
}
