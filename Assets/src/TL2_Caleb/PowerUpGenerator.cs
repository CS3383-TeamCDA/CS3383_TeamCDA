using UnityEngine;
using EscapeThe90s.PowerUps;

public class PowerUpGenerator : MonoBehaviour
{

    [SerializeField] private GameStateEventPublisher GameStateEvents;
    [SerializeField] private PowerUpManager manager;
    [SerializeField] private PowerUpType type = PowerUpType.Shield;
    [SerializeField] private Transform position;

    private bool _subscribed = false;

    private void Awake()
    {
    }

    private void OnEnable() {
        ManageSubscription(true);
    }

    private void OnDisable()
    {
        ManageSubscription(false);
    }

    private void GeneratePowerup()
    {
        manager.SpawnPowerUp(position.position, type);
        ManageSubscription(false);
    }

    private void ManageSubscription(bool desiredState)
    {
        if(_subscribed && !desiredState)
        {
            GameStateEvents.OnGameStarted -= GeneratePowerup;
            _subscribed = false;
        }

        if(!_subscribed && desiredState)
        {
            GameStateEvents.OnGameStarted += GeneratePowerup;
            _subscribed = true;
        }
    }
}
