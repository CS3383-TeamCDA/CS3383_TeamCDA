
using UnityEngine;
using Unity.Cinemachine;
using TL2_Dependencies;

public class CameraTargetBinder : DependentMonoBehaviour,
    IDependent<IGameStateEventPublisher>
{
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    private IGameStateEventPublisher _publisher;
    private bool _isSubscribed;

    protected override void Awake()
    {
        base.Awake();

        if (_cinemachineCamera == null)
            _cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    public void Inject(IGameStateEventPublisher publisher)
    {
        // Remove our listener from any previous publisher.
        Unsubscribe();

        _publisher = publisher;

        // Subscribe if the component is currently active.
        if (isActiveAndEnabled)
            Subscribe();
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
        if (_isSubscribed || _publisher == null)
            return;

        _publisher.OnPlayerSpawned += HandlePlayerSpawned;
        _isSubscribed = true;

        if (_publisher.CurrentPlayer != null)
        {
            HandlePlayerSpawned(_publisher.CurrentPlayer);
        }
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _publisher == null)
            return;

        _publisher.OnPlayerSpawned -= HandlePlayerSpawned;
        _isSubscribed = false;
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
