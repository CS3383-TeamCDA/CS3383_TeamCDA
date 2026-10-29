using System;
using UnityEngine;

public class DebugHUDController : MonoBehaviour
{
    [SerializeField] private FPSMonitor _fpsMonitor;
    [SerializeField] private ObjectSpawner _spawner;
    [SerializeField] private DebugHUDView _view;

    private int _spawnedCount = 0;
    
    private void OnEnable()
    {
        _fpsMonitor.FPSChanged += _view.SetFPS;
        _spawner.ObjectSpawned += OnObjectSpawned;
        _view.SetFPS(_fpsMonitor.FPS);
        _view.SetSpawnedCount(_spawnedCount);
    }

    private void OnDisable()
    {
        _fpsMonitor.FPSChanged -= _view.SetFPS;
        _spawner.ObjectSpawned -= OnObjectSpawned;
        _view.SetFPS(_fpsMonitor.FPS);
    }

    private void OnObjectSpawned(GameObject @object)
    {
        _spawnedCount++;
        _view.SetSpawnedCount(_spawnedCount);
    }
}