using System;
using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Append-only mapping from a global forward-run chunk index to streets in a WorldNetwork.</summary>
    public sealed class RunPath
    {
        readonly WorldNetwork _world;
        readonly List<RunPathStreet> _streets = new List<RunPathStreet>();
        long _nextGlobalChunk;

        public RunPath(WorldNetwork world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            _world = world;
        }

        public int StreetCount { get { return _streets.Count; } }
        public long ChunkCount { get { return _nextGlobalChunk; } }

        public RunPathStreet GetStreet(int index)
        {
            return index < 0 || index >= _streets.Count ? null : _streets[index];
        }

        /// <summary>Appends a connected surface street in traversal order.</summary>
        public bool TryAppendStreet(Int2 entryNode, Int2 destinationNode, out RunPathStreet segment)
        {
            segment = null;
            if (_streets.Count > 0 && !_streets[_streets.Count - 1].ExitNode.Equals(entryNode)) return false;

            WorldStreet street = _world.GetStreet(entryNode, destinationNode);
            if (street == null || street.Layout == null || street.Layout.ChunkCount <= 0) return false;
            if (long.MaxValue - _nextGlobalChunk < street.Layout.ChunkCount) return false;

            bool reversed = entryNode.Equals(street.Edge.B);
            segment = new RunPathStreet(_streets.Count, _nextGlobalChunk, street, entryNode, destinationNode, reversed);
            _streets.Add(segment);
            _nextGlobalChunk += street.Layout.ChunkCount;
            return true;
        }

        /// <summary>Finds the street chunk corresponding to a non-negative global run chunk index.</summary>
        public bool TryGetChunk(long globalChunkIndex, out RunChunkLocation location)
        {
            location = default(RunChunkLocation);
            if (globalChunkIndex < 0 || globalChunkIndex >= _nextGlobalChunk) return false;

            int low = 0;
            int high = _streets.Count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                RunPathStreet candidate = _streets[middle];
                if (globalChunkIndex < candidate.FirstGlobalChunk)
                {
                    high = middle - 1;
                    continue;
                }

                if (globalChunkIndex >= candidate.EndGlobalChunkExclusive)
                {
                    low = middle + 1;
                    continue;
                }

                int pathChunkOffset = (int)(globalChunkIndex - candidate.FirstGlobalChunk);
                int localIndex = candidate.Reversed
                    ? candidate.Street.Layout.ChunkCount - 1 - pathChunkOffset
                    : pathChunkOffset;
                location = new RunChunkLocation(globalChunkIndex, candidate, localIndex);
                return true;
            }

            return false;
        }

        /// <summary>Maps a street-local position to its monotonically increasing run coordinate.</summary>
        public bool TryGetPosition(RunPathStreet segment, float streetLocalX, out RunProgressPosition position)
        {
            return TryGetPosition(segment, streetLocalX, false, out position);
        }

        /// <summary>Maps a rendered street position; set forwardFacingLayout when run streets all render left-to-right.</summary>
        public bool TryGetPosition(RunPathStreet segment, float streetLocalX, bool forwardFacingLayout,
            out RunProgressPosition position)
        {
            position = default(RunProgressPosition);
            if (segment == null || segment.Index < 0 || segment.Index >= _streets.Count ||
                !ReferenceEquals(_streets[segment.Index], segment) || float.IsNaN(streetLocalX) ||
                float.IsInfinity(streetLocalX)) return false;

            double streetWidth = (double)segment.Street.Layout.ChunkCount * WorldConstants.ChunkWidthCells;
            double clampedX = streetLocalX;
            if (clampedX < 0d) clampedX = 0d;
            if (clampedX > streetWidth) clampedX = streetWidth;
            double traversedCells = !forwardFacingLayout && segment.Reversed ? streetWidth - clampedX : clampedX;
            double distance = (double)segment.FirstGlobalChunk * WorldConstants.ChunkWidthCells + traversedCells;
            position = new RunProgressPosition(distance);
            return true;
        }
    }
}
