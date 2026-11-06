using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Unity adapter that exposes a Transform through the shared engine-independent position contract.</summary>
    public sealed class TransformPlayerPositionSource : IPlayerPositionSource
    {
        readonly Transform _target;
        public TransformPlayerPositionSource(Transform target) { _target = target; }
        public WorldPosition2D GetPosition()
        {
            Vector3 position = _target == null ? Vector3.zero : _target.position;
            return new WorldPosition2D(position.x, position.y);
        }
    }
}
