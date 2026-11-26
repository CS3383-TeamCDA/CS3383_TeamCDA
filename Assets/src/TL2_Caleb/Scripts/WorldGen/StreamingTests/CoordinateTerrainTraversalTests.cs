#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using EscapeThe90s.Contracts;
using EscapeThe90s.WorldGen.Demo;
using EscapeThe90s.WorldGen.Integration;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateTerrainTraversalTests
    {
        private const long Seed = 431;
        private const string TraversalHeader = "seed,behavior,startX,targetX,reachedX,settledX,feetY,grounded,airborne,maxFeetY,maxSpeed,jumpSpeed,gravityScale,fallGravityMultiplier\n";
        private Scene _scene;
        private WorldGenSettings _settings;
        private Component _gameState;
        private GeneratedCoordinateContent _content;
        private PlayerControllerTL6 _player;
        private Collider2D _playerCollider;
        private CoordinateWorldStreamer _streamer;
        private TraversalInput _input;
        private Random.State _randomState;
        private static readonly WaitForFixedUpdate FixedStep = new WaitForFixedUpdate();

        [SetUp]
        public void CaptureRandomState() { _randomState = Random.state; Random.InitState((int)Seed); }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_content != null) _content.Dispose();
            if (_gameState != null) Object.Destroy(_gameState.gameObject);
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.CreateScene("AfterR03Traversal");
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            if (_settings != null) Object.Destroy(_settings);
            yield return null;
            Random.state = _randomState;
        }

        [UnityTest]
        public IEnumerator DefaultCoordinateRun_ShippedController_TraverseGroundRampAndLandAcrossGap()
        {
            yield return StartCoordinateRun();
            Directory.CreateDirectory("Temp/r03");
            File.WriteAllText("Temp/r03/traversal.csv", TraversalHeader);

            yield return Traverse("Ground", 18, 30, false, 0);

            int rampStart = 0;
            int rampEnd = 0;
            for (int join = -1; join <= 1; join++)
            {
                int left = join * 48 - 24;
                int right = join * 48 + 24;
                int leftHeight = _streamer.Profile.GetSurfaceHeight(left);
                int rightHeight = _streamer.Profile.GetSurfaceHeight(right);
                if (leftHeight == rightHeight) continue;
                rampStart = leftHeight < rightHeight ? left : right;
                rampEnd = leftHeight < rightHeight ? right : left;
                break;
            }
            Assert.AreNotEqual(rampStart, rampEnd, "R03 seed=431: expected an uphill connector in the safe opening.");
            yield return Traverse("Ramp", rampStart, rampEnd, false, 0);

            int firstGap = 96;
            while (firstGap < 2048 && (!_streamer.Profile.IsGapColumn(firstGap) || _streamer.Profile.IsGapColumn(firstGap - 1))) firstGap++;
            Assert.Less(firstGap, 2048, "R03 seed=431: no selected gap found.");
            int gapEnd = firstGap;
            while (_streamer.Profile.IsGapColumn(gapEnd)) gapEnd++;
            float gravity = Mathf.Abs(Physics2D.gravity.y) * _player.BaseGravityScale;
            float fullJumpRange = _player.MaxSpeedX * _player.JumpSpeed / gravity *
                (1f + 1f / Mathf.Sqrt(_player.FallGravityMultiplier));
            Assert.LessOrEqual(gapEnd - firstGap, fullJumpRange * 0.8f,
                "Gap width must fit the shipped full-jump range with a 20% margin.");
            yield return Traverse("Gap", firstGap - 5, gapEnd + 1, true, firstGap);
        }

        [UnityTest]
        public IEnumerator DefaultCoordinateRun_PlayerFallsIntoGap_ReturnsToSafeGroundWithCamera()
        {
            yield return StartCoordinateRun();
            int gap = 96;
            while (gap < 2048 && !_streamer.Profile.IsGapColumn(gap)) gap++;
            Assert.Less(gap, 2048);
            int safe = gap - 3;
            yield return Traverse("Approach", safe - 3, safe, false, 0);
            var camera = Object.FindAnyObjectByType<CameraFollow>();
            float groundCameraY = camera.transform.position.y;
            _player.Rb.position = new Vector2(gap + 0.5f, _streamer.SurfaceHeightAt(gap) + 8);
            _player.Rb.linearVelocity = Vector2.zero;
            yield return FixedStep;
            yield return null;
            yield return null;
            Assert.Greater(camera.transform.position.y, groundCameraY + 3f);
            float deadline = Time.realtimeSinceStartup + 8f;
            bool fellBelowRoad = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                fellBelowRoad |= _player.Rb.position.y < _streamer.SurfaceHeightAt(gap) - 4f;
                if (fellBelowRoad && _player.IsGrounded() && _player.Rb.position.x < gap - 1f) break;
                yield return null;
            }
            Assert.IsTrue(fellBelowRoad, "The real player must fall through the open shaft.");
            Assert.IsTrue(_player.IsGrounded());
            Assert.That(_player.Rb.position.x, Is.InRange((float)safe - 3f, (float)gap - 1f));
            Assert.That(camera.transform.position.y, Is.EqualTo(_player.transform.position.y).Within(0.2f));
        }

        [UnityTest]
        public IEnumerator DefaultCoordinateRun_FullHeightJumpAcrossChunkEdge_LandsAndContinues()
        {
            yield return StartCoordinateRun();
            const string report = "Temp/terrain-repair/default-jumps.csv";
            Directory.CreateDirectory("Temp/terrain-repair");
            File.WriteAllText(report, TraversalHeader);
            yield return Traverse("Held jump across X seam", 22, 43, true, 32, report);
            yield return Traverse("Second held jump", 44, 65, true, 54, report);
        }

        [UnityTest]
        public IEnumerator CoordinateRun_FlatRoadAtTopOfChunk_FullJumpReturnsToRoad()
        {
            yield return StartCoordinateRun(15);
            const string report = "Temp/terrain-repair/row-boundary-jump.csv";
            Directory.CreateDirectory("Temp/terrain-repair");
            File.WriteAllText(report, TraversalHeader);
            Assert.AreEqual(15, _streamer.SurfaceHeightAt(24));
            yield return Traverse("Held jump above vertical chunk boundary", 22, 43, true, 32, report);
        }

        private IEnumerator StartCoordinateRun(int? flatSurface = null)
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/GeneratedWorldGameDemo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            WorldNetworkDemo demo = null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (demo == null && Time.realtimeSinceStartup < deadline)
            {
                demo = Object.FindAnyObjectByType<WorldNetworkDemo>();
                if (demo == null) yield return null;
            }
            Assert.IsNotNull(demo, "R03 seed=431: scene composition missing.");
            Assert.IsTrue(demo.coordinateWorldMode, "The checked-in scene must still default to coordinate mode.");
            _settings = Object.Instantiate(demo.settings);
            _settings.seed = Seed;
            if (flatSurface.HasValue)
            {
                var serialized = new UnityEditor.SerializedObject(_settings);
                serialized.FindProperty("_worldSurfaceBaseline").intValue = flatSurface.Value;
                serialized.FindProperty("_worldSurfaceVariation").intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            demo.settings = _settings;
            IGameState state = null;
            foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (component is IGameState candidate) { state = candidate; _gameState = component; break; }
            Assert.IsNotNull(state);
            state.RequestStart();
            deadline = Time.realtimeSinceStartup + 10f;
            while ((_streamer == null || _streamer.Profile == null) && Time.realtimeSinceStartup < deadline)
            {
                _streamer = demo.GetComponent<CoordinateWorldStreamer>();
                yield return null;
            }
            Assert.IsNotNull(_streamer);
            CameraFollow camera = Object.FindAnyObjectByType<CameraFollow>();
            Assert.IsNotNull(camera);
            Assert.IsNotNull(camera.target);
            _player = camera.target.GetComponent<PlayerControllerTL6>();
            Assert.IsNotNull(_player, "Traversal must use the team's shipped movement controller.");
            _playerCollider = _player.GetComponent<Collider2D>();
            _input = new TraversalInput();
            _player.Input = _input;
            // Isolate terrain traversal from combat and pickup effects; the normal streamer,
            // player prefab, controller state machine, gravity and collision path remain active.
            _content = demo.GetComponent<GeneratedCoordinateContent>();
            Assert.IsNotNull(_content);
            _content.Dispose();
            Directory.CreateDirectory("Temp/r03");
        }

        private IEnumerator Traverse(string behavior, int start, int target, bool jump, int gapFirst, string report = "Temp/r03/traversal.csv")
        {
            string context = "R03 seed=431; behavior=" + behavior + "; start=" + start + "; target=" + target;
            _input.Set(Vector2.zero, false, false);
            _player.Rb.linearVelocity = Vector2.zero;
            _player.transform.position = new Vector3(start, _streamer.SurfaceHeightAt(start) + 3f, 0f);
            Physics2D.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!_streamer.IsRequiredWindowLoaded() || !_player.IsGrounded()) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(_streamer.IsRequiredWindowLoaded(), context + "; setup: terrain not ready");
            Assert.IsTrue(_player.IsGrounded(), context + "; setup: player did not settle");
            float initialFeet = _playerCollider.bounds.min.y;
            float maxFeet = initialFeet;
            bool airborne = false;
            bool airborneGroundQueryCleared = false;
            bool requestedJump = false;
            int direction = target > start ? 1 : -1;
            _input.Set(new Vector2(direction, 0f), false, false);
            deadline = Time.realtimeSinceStartup + 12f;
            while ((_player.transform.position.x - target) * direction < 0f && Time.realtimeSinceStartup < deadline)
            {
                if (jump && !requestedJump && _player.transform.position.x >= gapFirst - 1.5f)
                {
                    Assert.IsTrue(_player.IsGrounded(), context + "; takeoff: player left runway before jump");
                    requestedJump = true;
                    _input.Set(Vector2.right, true, true);
                }
                if (_player.Rb.linearVelocity.y > 1f)
                {
                    airborne = true;
                    _input.Set(new Vector2(direction, 0f), false, jump);
                }
                float currentFeet = _playerCollider.bounds.min.y;
                maxFeet = Mathf.Max(maxFeet, currentFeet);
                if (jump && currentFeet > initialFeet + 1f && !_player.IsGrounded()) airborneGroundQueryCleared = true;
                yield return null;
            }
            _input.Set(Vector2.zero, false, false);
            float reachedX = _player.transform.position.x;
            deadline = Time.realtimeSinceStartup + 5f;
            while ((!_player.IsGrounded() || Mathf.Abs(_player.Rb.linearVelocity.y) > 0.1f ||
                    Mathf.Abs(_player.Rb.linearVelocity.x) > 0.1f) && Time.realtimeSinceStartup < deadline) yield return null;
            // Let the position solver finish correcting first-contact penetration before recording the landing.
            yield return FixedStep;
            yield return FixedStep;
            float feet = _playerCollider.bounds.min.y;
            File.AppendAllText(report, string.Join(",", Seed, behavior, start, target,
                reachedX.ToString("F3", CultureInfo.InvariantCulture), _player.transform.position.x.ToString("F3", CultureInfo.InvariantCulture), feet.ToString("F3", CultureInfo.InvariantCulture),
                _player.IsGrounded(), airborne, maxFeet.ToString("F3", CultureInfo.InvariantCulture),
                _player.MaxSpeedX, _player.JumpSpeed, _player.BaseGravityScale, _player.FallGravityMultiplier) + "\n");
            Assert.GreaterOrEqual((reachedX - target) * direction, 0f, context + "; traversal: did not reach target; X=" + reachedX);
            Assert.IsTrue(_player.IsGrounded(), context + "; landing: player did not ground");
            Assert.That(feet, Is.EqualTo(_streamer.SurfaceHeightAt(Mathf.RoundToInt(_player.transform.position.x)) + 0.5f).Within(0.3f),
                context + "; landing: visible feet do not match ground");
            if (jump)
            {
                Assert.IsTrue(airborne, context + "; jump impulse was not observed");
                Assert.IsTrue(airborneGroundQueryCleared, context + "; airborne ground query must not detect the player's own collider");
                Assert.Greater(maxFeet, initialFeet + 1f, context + "; jump clearance insufficient");
            }
        }

        private sealed class TraversalInput : IPlayerMovementInput
        {
            public Vector2 Movement { get; private set; }
            public bool JumpJustPressed { get; private set; }
            public bool JumpBeingHeld { get; private set; }
            public bool JumpReleased => false;
            public bool DashJustPressed => false;
            public void Set(Vector2 movement, bool pressed, bool held)
            { Movement = movement; JumpJustPressed = pressed; JumpBeingHeld = held; }
        }
    }
}
#endif
