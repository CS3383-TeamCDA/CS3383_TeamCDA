using System;
using System.Collections.Generic;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using UnityEngine;

/// <summary>Owns one boss encounter at a time. Register this under IBossEncounter in Composition.</summary>
public sealed class BossEncounterController : DependencyBehaviour, IDependent<IGameState>, IBossEncounter
{
    [SerializeField] private BossEnemy bossPrefab;
    [SerializeField] private Transform player;
    private readonly HashSet<ulong> defeatedEncounters = new HashSet<ulong>();
    private IGameState gameState;
    private BossEnemy boss;
    private BossEncounterRequest activeRequest;
    public bool IsActive { get; private set; }
    public event Action<BossEncounterResult> OnEncounterEnded;

    public void Inject(IGameState dependency)
    {
        gameState = dependency;
    }

    public void Configure(BossEnemy prefab, Transform target)
    {
        if (IsActive)
        {
            throw new InvalidOperationException("Cannot reconfigure an active boss encounter.");
        }
        bossPrefab = prefab;
        player = target;
    }

    protected override void OnEnabledAfterInitialization()
    {
        gameState.OnStateTypeChanged += HandleStateChanged;
    }

    protected override void OnDisabledAfterInitialization()
    {
        gameState.OnStateTypeChanged -= HandleStateChanged;
        Cancel();
    }

    public bool TryStart(BossEncounterRequest request)
    {
        if (!IsInitialized || !isActiveAndEnabled || IsActive || gameState.CurrentStateType != GameStateType.Playing ||
            defeatedEncounters.Contains(request.EncounterId) || bossPrefab == null || request.Level < 1 ||
            float.IsNaN(request.Position.X) || float.IsInfinity(request.Position.X) ||
            float.IsNaN(request.Position.Y) || float.IsInfinity(request.Position.Y))
        {
            return false;
        }
        Transform target = player;
        if (target == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                target = playerObject.transform;
            }
        }
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }
        activeRequest = request;
        boss = Instantiate(bossPrefab, new Vector3(request.Position.X, request.Position.Y, 0f), Quaternion.identity, transform);
        boss.SetLevel(request.Level);
        boss.Configure(request.Seed, target);
        DependencyScope scope = GetComponentInParent<DependencyScope>();
        if (scope != null)
        {
            scope.Inject(boss.gameObject);
        }
        boss.Defeated += HandleBossDefeated;
        IsActive = true;
        return true;
    }

    private void Update()
    {
        if (IsActive && boss == null)
        {
            Finish(BossEncounterOutcome.Despawned);
        }
    }

    private void HandleStateChanged(GameStateChange change)
    {
        if (change.IsNewRun)
        {
            Cancel();
            defeatedEncounters.Clear();
        }
        else if (change.Current != GameStateType.Playing && change.Current != GameStateType.Paused)
        {
            Cancel();
        }
        if (boss != null)
        {
            boss.SetCombatActive(change.Current == GameStateType.Playing);
        }
    }

    private void HandleBossDefeated(EnemyBase defeated)
    {
        Finish(BossEncounterOutcome.Defeated);
    }

    public void Cancel()
    {
        if (IsActive)
        {
            Finish(BossEncounterOutcome.Cancelled);
        }
    }

    private void Finish(BossEncounterOutcome outcome)
    {
        if (!IsActive)
        {
            return;
        }
        BossEnemy finishedBoss = boss;
        BossEncounterRequest finishedRequest = activeRequest;
        IsActive = false;
        boss = null;
        int score = 0;
        int coins = 0;
        if (finishedBoss != null)
        {
            finishedBoss.Defeated -= HandleBossDefeated;
            if (outcome == BossEncounterOutcome.Defeated)
            {
                score = finishedBoss.ScoreValue;
                coins = finishedBoss.CoinReward;
                defeatedEncounters.Add(finishedRequest.EncounterId);
            }
            Destroy(finishedBoss.gameObject);
        }
        OnEncounterEnded?.Invoke(new BossEncounterResult(finishedRequest.EncounterId, outcome, score, coins));
    }
}
