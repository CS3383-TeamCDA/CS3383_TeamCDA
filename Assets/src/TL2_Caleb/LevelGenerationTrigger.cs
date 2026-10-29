using UnityEngine;
using TL2_Dependencies;

public class LevelGenerationTrigger : DependentMonoBehaviour, IDependent<IGameStateEventPublisher>
{
    [SerializeField] private LevelGenerator _levelGeneratorPrefab;

    private readonly EventSubscriptionGroup _eventGroup = new EventSubscriptionGroup();
    private LevelGenerator _level;

    public void Inject(IGameStateEventPublisher gsep)
    {
        // Clean inline allocation via composition—no manual tracking field needed
        _eventGroup.Add(HandleGameStarted, h => gsep.OnGameStarted += h, h => gsep.OnGameStarted -= h);
    }

    protected override void OnEnabledAfterInitialization() => _eventGroup.SubscribeAll();
    protected override void OnDisabledAfterInitialization() => _eventGroup.UnsubscribeAll();

    private void HandleGameStarted()
    {
        if (_level != null) 
        {
            Destroy(_level.gameObject); // Wipe old level geometry before generating a new one
        }

        _level = Instantiate(_levelGeneratorPrefab);
        _level.GenerateLevel();
    }
}