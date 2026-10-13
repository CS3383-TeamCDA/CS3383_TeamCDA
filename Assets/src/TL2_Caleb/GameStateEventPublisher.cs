using System;
using UnityEngine;
using EscapeThe90s.GameStates;
using EscapeThe90s;

public class GameStateEventPublisher : MonoBehaviour
{
    [SerializeField] private GameStateManager GSM;
    public event Action OnGameStarted;
    public event Action OnEnteredMenu;

    void OnEnable()
    {
        GSM.OnStateChanged += OnStateEnter;
    }

    void OnDisable()
    {
        GSM.OnStateChanged -= OnStateEnter;
    }

    void OnStateEnter(GameState GS)
    {
        switch (GS)
        {
            case PlayingState:
                GameStarted();
                break;

            case MenuState:
                EnteredMenu();
                break;

            case null:
                Debug.LogError("Game state is null!");
                break;

            default:
                Debug.LogWarning("Unkown State");
                break;
        }
    }

    private void GameStarted() {
        // Invoke only if subscribers exist
        OnGameStarted?.Invoke();
    }

    private void EnteredMenu() {
        // Invoke only if subscribers exist
        OnEnteredMenu?.Invoke();
    }
}
