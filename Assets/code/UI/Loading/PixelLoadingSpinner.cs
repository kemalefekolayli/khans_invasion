using UnityEngine;

public class PixelLoadingSpinner : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float stepDegrees = 45f;
    [SerializeField, Min(0.01f)] private float stepInterval = 0.1f;

    private float nextStepTime;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
        nextStepTime = Time.unscaledTime + stepInterval;
    }

    private void Update()
    {
        if (target == null || Time.unscaledTime < nextStepTime) return;

        int steps = Mathf.Max(1, Mathf.FloorToInt((Time.unscaledTime - nextStepTime) / stepInterval) + 1);
        target.localEulerAngles += Vector3.forward * stepDegrees * steps;
        nextStepTime += stepInterval * steps;
    }
}
