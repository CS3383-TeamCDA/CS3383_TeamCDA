using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Adapts a player Rigidbody2D to engine-independent position and velocity contracts.</summary>
    public sealed class RigidbodyPlayerMotionSource : IPlayerPositionSource, IPlayerVelocitySource
    {
        readonly Rigidbody2D _body;
        readonly Transform _fallbackTransform;

        public RigidbodyPlayerMotionSource(Rigidbody2D body, Transform fallbackTransform)
        {
            _body = body;
            _fallbackTransform = fallbackTransform;
        }

        public WorldPosition2D GetPosition()
        {
            Transform target = _body != null ? _body.transform : _fallbackTransform;
            if (target == null) return new WorldPosition2D(0f, 0f);
            Vector3 position = target.position;
            return new WorldPosition2D(position.x, position.y);
        }

        public WorldPosition2D GetVelocity()
        {
            if (_body == null) return new WorldPosition2D(0f, 0f);
#if UNITY_6000_0_OR_NEWER
            Vector2 velocity = _body.linearVelocity;
#else
            Vector2 velocity = _body.velocity;
#endif
            return new WorldPosition2D(velocity.x, velocity.y);
        }
    }
}
