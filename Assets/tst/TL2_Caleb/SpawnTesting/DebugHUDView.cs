using TMPro;
using UnityEngine;

public class DebugHUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text _fpsLabel;
    [SerializeField] private TMP_Text _spawnedLabel;

    public void SetFPS(int fps) => _fpsLabel.text = $"FPS: {fps}";
    public void SetSpawnedCount(int count) => _spawnedLabel.text = $"Spawned: {count}";
}