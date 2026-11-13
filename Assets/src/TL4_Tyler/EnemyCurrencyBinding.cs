using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;

/// <summary>Credits TL5 currency from the shared TL4 defeat contract.</summary>
public sealed class EnemyCurrencyBinding : DependencyBehaviour,
    IDependent<IEnemySpawner>, IDependent<IMetaProgressionManager>
{
    private IEnemySpawner spawner;
    private IMetaProgressionManager progression;

    public void Inject(IEnemySpawner dependency) { spawner = dependency; }
    public void Inject(IMetaProgressionManager dependency) { progression = dependency; }

    protected override void OnEnabledAfterInitialization()
    {
        spawner.OnEnemyDefeated += HandleDefeated;
    }

    protected override void OnDisabledAfterInitialization()
    {
        spawner.OnEnemyDefeated -= HandleDefeated;
    }

    private void HandleDefeated(EnemyDefeatedInfo info)
    {
        if (info.CoinReward > 0) progression.AddCurrency(info.CoinReward);
    }
}
