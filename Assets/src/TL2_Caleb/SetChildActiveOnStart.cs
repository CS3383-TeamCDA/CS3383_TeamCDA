using UnityEngine;
using TL2_Dependencies;
using EscapeThe90s; // Reference the namespace where GameStateManager lives
using EscapeThe90s.GameStates; // Reference where PlayingState lives

public class SetChildActiveOnStart : DependentMonoBehaviour, IDependent<IGameStateEventPublisher>
{
    [SerializeField] private GameObject childObject;
    
    private readonly EventSubscriptionGroup _eventGroup = new EventSubscriptionGroup();

    public void Inject(IGameStateEventPublisher gsep)
    {
        _eventGroup.Add(WakeChild, h => gsep.OnGameStarted += h, h => gsep.OnGameStarted -= h);
    }

    protected override void OnSetupComplete()
    {
        // 🚀 CATCH-UP CHECK: If the game has already started by the time this script is born,
        // bypass the event entirely and force the child to wake up immediately.
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState is PlayingState)
        {
            WakeChild();
        }
    }

    protected override void OnEnabledAfterInitialization() => _eventGroup.SubscribeAll();
    protected override void OnDisabledAfterInitialization() => _eventGroup.UnsubscribeAll();

    private void WakeChild()
    {
        if (childObject != null) 
        {
           // Debug.Log($"[SetChildActiveOnStart] Attempting to wake up: {childObject.name}. Current state before call: {childObject.activeSelf}", this);
            
            childObject.SetActive(true);
            
            //Debug.Log($"[SetChildActiveOnStart] Post-Activation check on {childObject.name}. activeSelf: {childObject.activeSelf}, activeInHierarchy: {childObject.activeInHierarchy}", this);
        }
    }
}