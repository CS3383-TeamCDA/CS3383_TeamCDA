using UnityEngine;

public class FPSCounter
{
    private float _smoothedDelta;
    private const float Smoothing = 0.1f;
    public int FPS => _smoothedDelta > 0f ? Mathf.RoundToInt(1f / _smoothedDelta) : 0;

    public void Tick(float unscaledDeltaTime)
    {
        _smoothedDelta = _smoothedDelta == 0f
            ? unscaledDeltaTime
            : Mathf.Lerp(_smoothedDelta, unscaledDeltaTime, Smoothing);
    }
}