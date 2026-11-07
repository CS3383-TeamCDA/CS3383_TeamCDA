using System.Reflection;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeThe90s.WorldGen.Integration
{
    /// <summary>Contains reflection-based bridges to team-owned runtime systems.</summary>
    public static class GeneratedWorldComposition
    {
        public static bool IsGameplayStarted()
        {
            foreach (MonoBehaviour component in FindAllBehaviours())
                if (component is IGameplaySessionState state) return state.IsGameplayStarted;
            return true;
        }

        public static bool HasTeamPlayerController(GameObject player)
        { return player != null && player.GetComponent<TempPlayerController>() != null; }

        public static void ConfigurePlayer(GameObject player, float moveSpeed)
        {
            if (player == null) return;
            TempPlayerController controller = player.GetComponent<TempPlayerController>();
            if (controller != null)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo groundLayer = controller.GetType().GetField("_groundLayer", flags);
                if (groundLayer != null && groundLayer.FieldType == typeof(LayerMask))
                {
                    int defaultLayer = LayerMask.NameToLayer("Default");
                    groundLayer.SetValue(controller, (LayerMask)(1 << (defaultLayer < 0 ? 0 : defaultLayer)));
                }
                FieldInfo speed = controller.GetType().GetField("_maxSpeedX", flags);
                if (speed != null && speed.FieldType == typeof(float) && moveSpeed > 0f)
                    speed.SetValue(controller, moveSpeed);
            }

            if (player.GetComponent<GeneratedPlayerPowerUpTarget>() == null)
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
