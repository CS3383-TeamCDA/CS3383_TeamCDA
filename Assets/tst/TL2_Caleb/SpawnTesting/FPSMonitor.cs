using System;
using UnityEngine;

public class FPSMonitor : MonoBehaviour
{
    [SerializeField] private float _refreshInterval = 0.25f;

    private readonly FPSCounter _counter = new FPSCounter();
    private float _timer;

    public int FPS { get; private set; }

    public event Action<int> FPSChanged;

    void Update()
    {
        _counter.Tick(Time.unscaledDeltaTime);
        _timer += Time.unscaledDeltaTime;
        if (_timer < _refreshInterval) return;

        _timer = 0f;
        FPS = _counter.FPS;
        FPSChanged?.Invoke(FPS);
    }
}