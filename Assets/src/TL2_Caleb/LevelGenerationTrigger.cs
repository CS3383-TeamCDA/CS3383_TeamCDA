using UnityEngine;

public class LevelGenerationTrigger : MonoBehaviour
{
    [SerializeField] private GameStateEventPublisher _gameStateEvents;
    [SerializeField] private LevelGenerator _levelGeneratorPrefab;

    private LevelGenerator _level;

    private void OnEnable() => _gameStateEvents.OnGameStarted += HandleGameStarted;
    private void OnDisable() => _gameStateEvents.OnGameStarted -= HandleGameStarted;

    private void HandleGameStarted()
    {
        if (_level != null) Destroy(_level.gameObject);   // restart: clear the old level

        _level = Instantiate(_levelGeneratorPrefab);
        _level.GenerateLevel();
    }
}