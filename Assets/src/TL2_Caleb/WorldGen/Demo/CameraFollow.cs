using EscapeThe90s.WorldGen;
using UnityEngine;

// DEMO ONLY (see DemoWalker). Pixel-snapped side-scroll camera clamped to the street.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public StreetView street;      // optional: clamps the camera to the street ends

    void LateUpdate()
    {
        if (target == null) return;
        Camera cam = GetComponent<Camera>();
        float halfW = cam.orthographicSize * cam.aspect;
        float x = target.position.x;
        if (street != null && street.WidthCells > 0)
        {
            float width = street.WidthCells;
            x = width > 2f * halfW ? Mathf.Clamp(x, halfW, width - halfW) : width * 0.5f;
        }
        x = Mathf.Round(x * WorldConstants.CellPixels) / WorldConstants.CellPixels;   // snap to whole pixels
        transform.position = new Vector3(x, StreetView.CameraY(cam.orthographicSize), -10f);
    }
}
