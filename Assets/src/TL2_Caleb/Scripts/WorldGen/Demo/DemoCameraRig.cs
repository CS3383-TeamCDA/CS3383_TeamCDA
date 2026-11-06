using UnityEngine;

namespace EscapeThe90s.WorldGen.Demo
{
    /// <summary>Configures the bounded pixel-snapped camera used by the playable street demo.</summary>
    public sealed class DemoCameraRig
    {
        public CameraFollow Attach(Transform target, StreetView street)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = StreetView.DefaultOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.05f, 0.13f);

            // The existing Cinemachine rig has no bounds for generated streets.
            Component brain = camera.GetComponent("CinemachineBrain");
            if (brain is Behaviour) ((Behaviour)brain).enabled = false;

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.target = target;
            follow.street = street;
            return follow;
        }
    }
}
