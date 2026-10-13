using UnityEngine;

public class LevelGenerator : MonoBehaviour
{

    [SerializeField] private GameStateEventPublisher GameStateEvents;
    [SerializeField] private ChunkSpawner StartChunkGen;
    [SerializeField] private ChunkSpawner MiddleChunkGen;
    [SerializeField] private ChunkSpawner EndChunkGen;
    private bool _subscribed = false;

    private void Awake()
    {
        if (StartChunkGen == null || MiddleChunkGen == null || EndChunkGen == null)
        {
            Debug.LogError("A chunk gen is missing!");
        }
    }

    private void OnEnable() {
        ManageSubscription(true);
    }

    private void OnDisable()
    {
        ManageSubscription(false);
    }

    private void GenerateLevel()
    {
        StartChunkGen.SpawnChunkAtCoordinates(0,0);
        MiddleChunkGen.SpawnChunkAtCoordinates(1,0);
        EndChunkGen.SpawnChunkAtCoordinates(2,0);
        ManageSubscription(false);
    }

    private void ManageSubscription(bool desiredState)
    {
        if(_subscribed && !desiredState)
        {
            GameStateEvents.OnGameStarted -= GenerateLevel;
            _subscribed = false;
        }

        if(!_subscribed && desiredState)
        {
            GameStateEvents.OnGameStarted += GenerateLevel;
            _subscribed = true;
        }
    }
}
