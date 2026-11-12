namespace EscapeThe90s.WorldGen
{
    /// <summary>Tracks the furthest global position reached along a RunPath.</summary>
    public sealed class RunProgress
    {
        readonly DifficultyProgression _difficulty;
        RunProgressPosition _furthestPosition;
        bool _hasAdvanced;

        public RunProgress(DifficultyProgression difficulty = null)
        {
            _difficulty = difficulty ?? new DifficultyProgression();
        }

        public RunProgressPosition FurthestPosition { get { return _furthestPosition; } }
        public double GetCurrentDistanceInCells() { return _hasAdvanced ? _furthestPosition.DistanceInCells : 0d; }
        public float GetCurrentDistance()
        {
            double distance = GetCurrentDistanceInCells();
            return distance >= float.MaxValue ? float.MaxValue : (float)distance;
        }
        public float GetCurrentDifficulty() { return _difficulty.DifficultyAt(GetCurrentDistance()); }

        /// <summary>Advances progress only when the new path position is farther than the previous maximum.</summary>
        public bool Advance(RunProgressPosition position)
        {
            if (double.IsNaN(position.DistanceInCells) || double.IsInfinity(position.DistanceInCells) ||
                (_hasAdvanced && position.DistanceInCells <= _furthestPosition.DistanceInCells)) return false;
            _furthestPosition = position;
            _hasAdvanced = true;
            return true;
        }

        public void SetDifficulty(float difficulty)
        {
            _difficulty.Rebase(GetCurrentDistance(), difficulty);
        }
    }
}
