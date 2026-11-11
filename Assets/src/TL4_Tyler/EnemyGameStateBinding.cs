using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using UnityEngine;

/// <summary>Scene integration adapter; standalone enemy demos do not require game services.</summary>
[RequireComponent(typeof(EnemySpawner))]
public sealed class EnemyGameStateBinding : DependencyBehaviour, IDependent<IGameState>
{
    private IGameState gameState;
    private EnemySpawner spawner;

    protected override void Awake()
    {
        base.Awake();
        spawner = GetComponent<EnemySpawner>();
        spawner.SetGameplaySpawningAllowed(false);
    }

    public void Inject(IGameState dependency) { gameState = dependency; }

    protected override void OnEnabledAfterInitialization()
    {
        gameState.OnStateTypeChanged += HandleStateChanged;
        ApplyState(gameState.CurrentStateType);
    }

    protected override void OnDisabledAfterInitialization()
    {
        gameState.OnStateTypeChanged -= HandleStateChanged;
        spawner.SetGameplaySpawningAllowed(false);
    }

    private void HandleStateChanged(GameStateChange change) { ApplyState(change.Current); }
    private void ApplyState(GameStateType state)
    {
        spawner.SetGameplaySpawningAllowed(state == GameStateType.Playing);
    }
}
