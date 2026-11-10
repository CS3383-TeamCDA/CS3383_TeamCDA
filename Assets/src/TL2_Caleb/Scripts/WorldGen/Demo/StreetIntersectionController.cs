using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Demo
{
    /// <summary>Owns the active intersection, its valid exits, and route selection state.</summary>
    public sealed class StreetIntersectionController
    {
        public Int2 ActiveIntersection { get; private set; }
        public IntersectionExit[] Exits { get; private set; } = new IntersectionExit[0];
        public int SelectedExit { get; private set; }
        public bool IsAtIntersection { get; private set; }
        public bool IsVirtualIntersection { get; private set; }

        public void ResetForStreet()
        {
            IsVirtualIntersection = false;
            IsAtIntersection = false;
            Exits = new IntersectionExit[0];
            SelectedExit = 0;
        }

        public void ShowVirtualIntersection(WorldNetwork world, Int2 station)
        {
            ActiveIntersection = station;
            Exits = world.GetIntersection(station);
            SelectedExit = 0;
            IsVirtualIntersection = true;
            IsAtIntersection = Exits.Length > 0;
        }

        public string UpdateStreetPrompt(WorldNetwork world, WorldStreet street, Transform player,
            Int2 entryNode, Int2 destinationNode, float entryX, float destinationX)
        {
            if (IsVirtualIntersection)
            {
                IsAtIntersection = Exits.Length > 0;
                return null;
            }
            float x = player.position.x;
            float toEntry = Mathf.Abs(x - entryX);
            float toDestination = Mathf.Abs(x - destinationX);
            const float promptDistance = 7.5f;
            bool nearEntry = toEntry <= promptDistance;
            bool nearDestination = toDestination <= promptDistance;
            if (!nearEntry && !nearDestination)
            {
                IsAtIntersection = false;
                return null;
            }

            Int2 node = toEntry <= toDestination ? entryNode : destinationNode;
            if (!IsAtIntersection || !ActiveIntersection.Equals(node))
            {
                ActiveIntersection = node;
                IntersectionExit[] allExits = world.GetIntersection(node);
                var onwardRoutes = new List<IntersectionExit>(allExits.Length);
                foreach (IntersectionExit exit in allExits)
                    if (exit.IsSubway || !exit.StreetId.Equals(street.Request.Id)) onwardRoutes.Add(exit);
                Exits = onwardRoutes.ToArray();
                SelectedExit = 0;
                IsAtIntersection = Exits.Length > 0;
                return Exits.Length == 0
                    ? "No other routes meet this end. Walk back along the street to return."
                    : "Choose a route connected to this end of the street.";
            }
            IsAtIntersection = Exits.Length > 0;
            return null;
        }

        public void SelectBy(int direction)
        {
            if (direction == 0 || Exits.Length == 0) return;
            SelectedExit = (SelectedExit + direction + Exits.Length) % Exits.Length;
        }

        public void Select(int index)
        {
            if (Exits.Length == 0) { SelectedExit = 0; return; }
            SelectedExit = Mathf.Clamp(index, 0, Exits.Length - 1);
        }
    }
}
