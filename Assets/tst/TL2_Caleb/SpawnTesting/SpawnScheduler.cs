using UnityEngine;

public class SpawnScheduler : MonoBehaviour
{
    [SerializeField] private ObjectSpawner _spawner;
    [SerializeField] private SpawnPattern _pattern;
    [SerializeField] private FPSMonitor _fpsMonitor;
    [SerializeField] private float _waveInterval = 1f;
    [SerializeField] private int _stopAtFps = 60;

    private float _timer;
    private int _wave;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _waveInterval) return;

        _timer = 0f;

        int fps = _fpsMonitor.FPS;
        if (fps > 0 && fps <= _stopAtFps)
        {
            Debug.Log($"Spawning stopped at {fps} FPS after {_wave} waves.");
            enabled = false;
            return;
        }

        int count = _pattern.GetCount(_wave++);
        for (int i = 0; i < count; i++)
            _spawner.Spawn();
    }
}