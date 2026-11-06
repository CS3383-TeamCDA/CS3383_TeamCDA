using UnityEngine;
using EscapeThe90s.WorldGen.Integration;

namespace EscapeThe90s.WorldGen.Demo
{
    /// <summary>Creates and configures the player object used by the playable generated-world demo.</summary>
    public sealed class DemoPlayerRig
    {
        public Transform Player { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public bool OwnsPlayer { get; private set; }

        public static DemoPlayerRig Create(Transform owner, Transform assignedPlayer, GameObject prefab, float teamPlayerSpeed)
        {
            GameObject playerObject;
            bool ownsPlayer = false;
            if (assignedPlayer != null) playerObject = assignedPlayer.gameObject;
            else if (prefab != null)
            {
                playerObject = Object.Instantiate(prefab);
                playerObject.name = prefab.name + " (World Network Demo)";
                ownsPlayer = true;
            }
            else
            {
                playerObject = new GameObject("Demo Player");
                playerObject.transform.SetParent(owner, false);
                ownsPlayer = true;
            }

            Rigidbody2D body = playerObject.GetComponent<Rigidbody2D>();
            bool addedBody = body == null;
            if (addedBody) body = playerObject.AddComponent<Rigidbody2D>();
            if (addedBody)
            {
                body.gravityScale = 3f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            CapsuleCollider2D capsule = playerObject.GetComponent<CapsuleCollider2D>();
            if (capsule == null) capsule = playerObject.AddComponent<CapsuleCollider2D>();
            DemoWalker walker = playerObject.GetComponent<DemoWalker>();
            bool teamPlayer = GeneratedWorldComposition.HasTeamPlayerController(playerObject);
            if (walker == null && !teamPlayer) walker = playerObject.AddComponent<DemoWalker>();
            if (walker != null)
            {
                walker.moveSpeed = 18f;
                capsule.size = new Vector2(0.75f, 1.25f);
                capsule.offset = new Vector2(0f, 0.625f);
                capsule.sharedMaterial = new PhysicsMaterial2D("WorldNetworkDemoNoFriction") { friction = 0f, bounciness = 0f };
            }
            playerObject.tag = "Player";
            GeneratedWorldComposition.ConfigurePlayer(playerObject, teamPlayerSpeed);
            SpriteRenderer renderer = playerObject.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sortingOrder = 5;
            return new DemoPlayerRig { Player = playerObject.transform, Body = body, OwnsPlayer = ownsPlayer };
        }
    }
}
