
using UnityEngine;
using Unity.Cinemachine;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;

public class CameraTargetBinder : DependencyBehaviour, IDependent<IEventBus>
{
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    private IEventBus _events;
    private System.IDisposable _subscription;

    protected override void Awake()
    {
        base.Awake();

        if (_cinemachineCamera == null)
            _cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    public void Inject(IEventBus events)
    {
        // Remove our listener from any previous publisher.
        Unsubscribe();
        _events = events;
        if (isActiveAndEnabled) Subscribe();
    }

    protected override void OnEnabledAfterInitialization()
    {
        Subscribe();
    }

    protected override void OnDisabledAfterInitialization()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscription != null || _events == null)
            return;
        _subscription = _events.Subscribe<UnityPlayerSpawnedEvent>(message => HandlePlayerSpawned(message.Player), true);
    }

    private void Unsubscribe()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private void HandlePlayerSpawned(Transform playerTransform)
    {
        if (_cinemachineCamera == null || playerTransform == null)
        {
            Debug.LogWarning("CameraTargetBinder: Missing camera or player target.");
            return;
        }
        _cinemachineCamera.Follow = playerTransform;
    }
}
