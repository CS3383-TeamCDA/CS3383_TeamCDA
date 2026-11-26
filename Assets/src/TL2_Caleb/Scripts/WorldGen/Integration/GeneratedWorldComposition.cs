using EscapeThe90s.PowerUps;
using EscapeThe90s.Infrastructure.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeThe90s.WorldGen.Integration
{
    /// <summary>Wires player services for the generated world.</summary>
    public static class GeneratedWorldComposition
    {
        public static bool HasTeamPlayerController(GameObject player)
        { return player != null && player.GetComponent<PlayerControllerTL6>() != null; }

        public static void ConfigurePlayer(GameObject player)
        {
            if (player == null) return;
            if (player.GetComponent<IPowerUpTarget>() == null)
                player.AddComponent<GeneratedPlayerPowerUpTarget>();

            DependencyScope scope = Object.FindAnyObjectByType<DependencyScope>(FindObjectsInactive.Include);
            if (scope != null) scope.Inject(player);
        }

        internal static MonoBehaviour[] FindSceneBehaviours(Scene scene)
        {
            var matches = new System.Collections.Generic.List<MonoBehaviour>();
            foreach (MonoBehaviour component in FindAllBehaviours())
                if (component != null && component.gameObject.scene == scene) matches.Add(component);
            return matches.ToArray();
        }

        internal static MonoBehaviour[] FindAllBehaviours()
        {
            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        }
    }
}
