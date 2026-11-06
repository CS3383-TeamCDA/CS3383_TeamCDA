using EscapeThe90s.WorldGen;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Demo
{
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
        if (street != null && street.TryGetWorldXBounds(out float minX, out float maxX))
        {
            x = maxX - minX > 2f * halfW
                ? Mathf.Clamp(x, minX + halfW, maxX - halfW)
                : (minX + maxX) * 0.5f;
        }
        x = Mathf.Round(x * WorldConstants.CellPixels) / WorldConstants.CellPixels;   // snap to whole pixels
        if (street != null && street.TryGetWorldXBounds(out minX, out maxX))
            x = maxX - minX > 2f * halfW
                ? Mathf.Clamp(x, minX + halfW, maxX - halfW)
                : (minX + maxX) * 0.5f;
        transform.position = new Vector3(x, StreetView.CameraY(cam.orthographicSize), -10f);
    }
}
}
