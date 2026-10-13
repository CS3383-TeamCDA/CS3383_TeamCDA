using UnityEngine;

public class SetChildActiveOnStart : MonoBehaviour
{
    [SerializeField] private GameObject childObject; // The object that toggles
    [SerializeField] private GameStateEventPublisher GameStateEvents;

    private void OnEnable()
    {
        GameStateEvents.OnGameStarted += WakeChild;
    }

    private void OnDisable()
    {
        GameStateEvents.OnGameStarted -= WakeChild;
    }

    private void WakeChild()
    {
        if (childObject != null)
        {
            childObject.SetActive(true); // Works perfectly because ParentManager is still active!
        }
    }
}