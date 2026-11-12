using EscapeThe90s.Contracts;
using UnityEngine;
using UnityEngine.Serialization;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Streams pooled coordinate chunks around a player's world position.</summary>
    public sealed class CoordinateWorldStreamer : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("art")] private WorldArt _art;
        [SerializeField, FormerlySerializedAs("target")] private Transform _target;
        private bool _showChunkDebug;

        SideScrollerWorldProfile _profile;
        SideScrollerStreamPolicy _policy;
        RigidbodyPlayerMotionSource _motionSource;
        LevelProgress _progress;
        CoordinateWorldChunkStore _store;
        float _originX;
        bool _initialized;
        bool _loggedSpeedCap;

        public int LoadedChunkCount { get { return _store == null ? 0 : _store.LoadedCount; } }
        public ChunkPoolMetrics PoolMetrics
        { get { return _store == null ? new ChunkPoolMetrics(0, 0, 0, 0, 0) : _store.PoolMetrics; } }
        public ILevelProgress Progress { get { return _progress; } }
        public SideScrollerWorldProfile Profile { get { return _profile; } }

        public void Initialize(WorldGenSettings settings, WorldArt worldArt, Transform target, Rigidbody2D body)
        {
            if (settings == null || worldArt == null || target == null)
            {
                Debug.LogError("CoordinateWorldStreamer needs world settings, art, and a target.", this);
                return;
            }
            if (_store != null) _store.Clear();

            _art = worldArt;
            _target = target;
            _profile = new SideScrollerWorldProfile(settings.SeedValue, settings.WorldSurfaceBaseline,
                settings.WorldSurfaceVariation, settings.WorldSurfaceAnchorSpan, settings.WorldFloorCellY,
                settings.DistancePerDifficulty, settings.BiomeCount, settings.WorldBiomeRegionSpanChunks,
                settings.WorldCityBlockLengthCells, settings.WorldCityBlockFlatHalfWidthCells);
            _policy = new SideScrollerStreamPolicy(settings.StreamBehindChunks,
                settings.StreamBaseAheadChunks, settings.StreamMaxAheadChunks,
                settings.StreamVerticalRadius, settings.StreamLookaheadSeconds);
            _motionSource = new RigidbodyPlayerMotionSource(body, target);
            _progress = new LevelProgress(new DifficultyProgression(settings.DistancePerDifficulty));
            _originX = _motionSource.GetPosition().X;

            int columns = settings.StreamBehindChunks + settings.StreamMaxAheadChunks +
                settings.StreamHysteresisChunks * 2 + 1;
            int rows = settings.StreamVerticalRadius * 2 + 1;
            _store = new CoordinateWorldChunkStore(transform, _art, _profile, _progress,
                settings.StreamBuildBudget, settings.StreamHysteresisChunks,
                settings.CoordinateChunkPoolSize, columns * rows);
            _initialized = true;
        }

        void LateUpdate()
        { if (_initialized) UpdateStreaming(); }

        void UpdateStreaming()
        {
            WorldPosition2D position = _motionSource.GetPosition();
            WorldPosition2D velocity = _motionSource.GetVelocity();
            WorldStreamWindow window = _policy.GetWindow(position, velocity);
            _progress.AdvanceDistance(Mathf.Max(0f, position.X - _originX));
            if (window.IsCapped && !_loggedSpeedCap)
            {
                Debug.LogWarning("Coordinate streaming reached its configured lookahead cap.", this);
                _loggedSpeedCap = true;
            }
            _store.Stream(window, _originX);
        }

        public void SetDifficulty(float difficulty)
        { if (_progress != null) _progress.SetDifficulty(difficulty); }

        public int SurfaceHeightAt(int worldX)
        { return _profile == null ? WorldConstants.WorldSurfaceBaseline : _profile.GetSurfaceHeight(worldX); }

        /// <summary>Enables or disables chunk bounds in the editor Gizmo views.</summary>
        public void SetChunkDebugEnabled(bool enabled)
        { _showChunkDebug = enabled; }

        void OnDrawGizmos()
        {
            if (_showChunkDebug && _store != null) _store.DrawDebugGizmos();
        }

        void OnDestroy()
        {
            _initialized = false;
            if (_store != null) _store.Clear();
        }
    }
}
