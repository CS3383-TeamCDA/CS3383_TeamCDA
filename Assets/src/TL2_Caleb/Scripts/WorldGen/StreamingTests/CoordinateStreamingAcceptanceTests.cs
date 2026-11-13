#if UNITY_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s.WorldGen.Integration;
using Object = UnityEngine.Object;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateStreamingAcceptanceTests
    {
        const int ChunkCount = 2000;
        const int WarmupChunks = 64;
        const int StartChunk = -1000;
        const float BaselineSpeed = 16f;
        GameObject _root;
        WorldGenSettings _settings;
        GeneratedCoordinateContent _content;
        UnityEngine.Random.State _randomState;

        [SetUp]
        public void CaptureRandomState() { _randomState = UnityEngine.Random.state; }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_content != null) _content.Dispose();
            if (_root != null) Object.Destroy(_root);
            if (_settings != null) Object.Destroy(_settings);
            yield return null;
            UnityEngine.Random.state = _randomState;
        }

        [UnityTest]
        public IEnumerator ST1ST3ST4_TerrainOnly_AllSpeedsMeetStreamingAcceptance()
        { return RunSpeeds(false); }

        [UnityTest]
        public IEnumerator ST1ST3ST4_SpawnedContent_AllSpeedsMeetStreamingAcceptance()
        { return RunSpeeds(true); }

        [Test]
        public void Stream_ReplacementWindow_ReusesRetiringViewBeforeRent()
        {
            _root = new GameObject("Single-view replacement regression");
            WorldArt art = AssetDatabase.LoadAssetAtPath<WorldArt>("Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset");
            var store = new CoordinateWorldChunkStore(_root.transform, art, new SideScrollerWorldProfile(431UL),
                new LevelProgress(), 1, 0, 1, 1);
            store.Stream(new WorldStreamWindow(new WorldChunkCoord(0, 0), 0, 0, 0, 0, 1, 1, false), 0f);
            Assert.AreEqual(1, store.PoolMetrics.Created);
            store.Stream(new WorldStreamWindow(new WorldChunkCoord(1, 0), 1, 1, 0, 0, 1, 1, false), 0f);
            Assert.AreEqual(1, store.LoadedCount);
            Assert.AreEqual(1, store.PoolMetrics.Created, "Returning the retired view must precede renting the replacement.");
            store.Clear();
        }

        [Test]
        public void StreamOnce_SettledWindow_RecordsZeroAllocationEvents()
        {
            _root = new GameObject("Settled streaming allocation regression");
            var target = new GameObject("Target");
            target.transform.SetParent(_root.transform);
            target.transform.position = new Vector3(0f, 8f, 0f);
            _settings = ScriptableObject.CreateInstance<WorldGenSettings>();
            WorldArt art = AssetDatabase.LoadAssetAtPath<WorldArt>("Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset");
            var streamer = _root.AddComponent<CoordinateWorldStreamer>();
            streamer.enabled = false;
            streamer.Initialize(_settings, art, target.transform, null);
            streamer.MeasurePerformance = true;
            for (int i = 0; i < 16; i++) streamer.StreamOnce();
            Assert.IsTrue(streamer.IsRequiredWindowLoaded());
            long allocationEvents = 0;
            for (int i = 0; i < 1000; i++)
            {
                streamer.StreamOnce();
                allocationEvents += streamer.LastSample.TerrainAllocationEvents + streamer.LastSample.ContentAllocationEvents;
            }
            Assert.AreEqual(0, allocationEvents);
            Assert.IsFalse(streamer.AllocationProbeOverflowed);
        }

        IEnumerator RunSpeeds(bool contentEnabled)
        {
            long calibrationEvents;
            using (var calibration = new CoordinateAllocationProbe())
            {
                Assert.IsTrue(calibration.IsValid, "GC.Alloc event counter is unavailable.");
                calibration.Begin();
                var deliberateAllocation = new byte[4096];
                GC.KeepAlive(deliberateAllocation);
                calibrationEvents = calibration.End();
                Assert.Greater(calibrationEvents, 0, "Allocation counter failed positive-control calibration.");
            }
            var referenceHashes = new ulong[ChunkCount];
            for (int multiplier = 1; multiplier <= 4; multiplier *= 2)
            {
                UnityEngine.Random.InitState(431); // Team prefab variation is repeatable and restored by teardown.
                _root = new GameObject("Coordinate Streaming Acceptance");
                var target = new GameObject("Fixed-step stream target");
                target.transform.SetParent(_root.transform);
                var cameraObject = new GameObject("Streaming acceptance camera");
                cameraObject.transform.SetParent(_root.transform);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 11.25f;
                camera.tag = "MainCamera";
                Rigidbody2D body = target.AddComponent<Rigidbody2D>();
                body.simulated = false;
                float speed = BaselineSpeed * multiplier;
                body.linearVelocity = new Vector2(speed, 0f);
                target.transform.position = new Vector3(StartChunk * 16, 8f, 0f);
                _settings = ScriptableObject.CreateInstance<WorldGenSettings>();
                _settings.seed = 431;
                WorldArt art = AssetDatabase.LoadAssetAtPath<WorldArt>("Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset");
                Assert.IsNotNull(art);
                var streamer = _root.AddComponent<CoordinateWorldStreamer>();
                streamer.enabled = false; // Only explicit fixed 1/60 s steps advance the production streamer.
                streamer.Initialize(_settings, art, target.transform, body);
                if (contentEnabled)
                {
                    GameObject powerUps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/prefabs/TL3_Joe/PowerUps/PowerUpSystem.prefab"), _root.transform);
                    Assert.IsNotNull(powerUps);
                    string[] guids = { "2f6a23a66998f4ebbbac180c85c633cb", "3f4f7257c1046461594f3f310cdc5921", "d45a3f8f106404f2593561d5678976b6" };
                    var prefabs = new GameObject[guids.Length];
                    for (int i = 0; i < guids.Length; i++)
                    {
                        prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                        Assert.IsNotNull(prefabs[i]);
                    }
                    _content = _root.AddComponent<GeneratedCoordinateContent>();
                    _content.Initialize(streamer, art, target.transform, prefabs);
                }
                // Cold start is settled explicitly, then 64 whole chunks warm every runtime cache.
                for (int settle = 0; settle < 100; settle++)
                { streamer.StreamOnce(); if (streamer.IsRequiredWindowLoaded()) break; }
                Assert.IsTrue(streamer.IsRequiredWindowLoaded(), "Cold-start window never became ready.");
                int warmupSteps = WarmupChunks * 16 * 60 / (int)speed;
                for (int step = 1; step <= warmupSteps; step++)
                {
                    target.transform.position = new Vector3(StartChunk * 16 + step * speed / 60f, 8f, 0f);
                    camera.transform.position = new Vector3(target.transform.position.x, 8.25f, -10f);
                    streamer.StreamOnce();
                    if (step % 600 == 0) yield return null;
                }
                yield return null;
                streamer.MeasurePerformance = true;
                streamer.StreamOnce(); // Warm the optional instrumentation too, before the pool/measurement baseline.
                Assert.AreEqual(Mathf.CeilToInt(_settings.StreamBaseAheadChunks + speed * _settings.StreamLookaheadSeconds / 16),
                    streamer.CurrentWindow.LookaheadChunks, "The velocity adapter must actually drive the speed-expanded window.");
                ChunkPoolMetrics initial = streamer.PoolMetrics;
                using var frameAllocations = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,
                    "GC Allocated In Frame", 1);
                Assert.IsTrue(frameAllocations.Valid);
                int steps = ChunkCount * 16 * 60 / (int)speed;
                var terrainTimes = new long[steps];
                var contentTimes = new long[steps];
                long terrainAllocationEvents = 0, contentAllocationEvents = 0, maximumTerrainAllocationEvents = 0, maximumContentAllocationEvents = 0;
                int peakLoaded = 0, missedEdges = 0, missingHashes = 0, differentHashes = 0, lastRecorded = -1;
                float maximumObservedFrameMs = 0;
                long maximumWholeFrameBytes = 0;
                yield return null; // Finish the setup-allocation frame before collecting frame context.
                for (int step = 0; step < steps; step++)
                {
                    float x = (StartChunk + WarmupChunks) * 16 + step * speed / 60f;
                    target.transform.position = new Vector3(x, 8f, 0f);
                    camera.transform.position = new Vector3(x, 8.25f, -10f);
                    streamer.StreamOnce(); // Timed/allocated section is inside StreamOnce, not the harness.
                    CoordinateStreamingSample sample = streamer.LastSample;
                    terrainTimes[step] = sample.TerrainTicks;
                    contentTimes[step] = sample.ContentTicks;
                    terrainAllocationEvents += sample.TerrainAllocationEvents;
                    contentAllocationEvents += sample.ContentAllocationEvents;
                    maximumTerrainAllocationEvents = Math.Max(maximumTerrainAllocationEvents, sample.TerrainAllocationEvents);
                    maximumContentAllocationEvents = Math.Max(maximumContentAllocationEvents, sample.ContentAllocationEvents);
                    peakLoaded = Math.Max(peakLoaded, streamer.LoadedChunkCount);
                    // Required edge is the FULL speed-expanded window, not just the camera footprint.
                    if (!streamer.IsRequiredWindowLoaded()) missedEdges++;
                    int index = streamer.CurrentWindow.Center.X - StartChunk - WarmupChunks;
                    if (index >= 0 && index < ChunkCount && index != lastRecorded)
                    {
                        if (!streamer.TryGetContentHash(new WorldChunkCoord(StartChunk + WarmupChunks + index, 0), out ulong hash)) missingHashes++;
                        else if (multiplier == 1) referenceHashes[index] = hash;
                        else if (hash != referenceHashes[index]) differentHashes++;
                        lastRecorded = index;
                    }
                    if (step % 600 == 599)
                    {
                        yield return null; // Flush deferred content destruction; overhead is outside the timed step.
                        maximumObservedFrameMs = Math.Max(maximumObservedFrameMs, Time.unscaledDeltaTime * 1000f);
                        maximumWholeFrameBytes = Math.Max(maximumWholeFrameBytes, frameAllocations.LastValue);
                    }
                }
                ChunkPoolMetrics final = streamer.PoolMetrics;
                int requests = final.RentRequests - initial.RentRequests;
                int hits = final.PoolHits - initial.PoolHits;
                // Separate real-rendered-frame context: one simulated step per frame, no report/log construction.
                // Includes Unity Test Runner / Editor frame overhead; scoped streaming CPU remains separately measured.
                var renderedFrames = new float[240];
                for (int frame = 0; frame < renderedFrames.Length; frame++)
                {
                    target.transform.position += new Vector3(speed / 60f, 0f, 0f);
                    camera.transform.position = new Vector3(target.transform.position.x, 8.25f, -10f);
                    streamer.StreamOnce();
                    yield return null;
                    renderedFrames[frame] = Time.unscaledDeltaTime * 1000f;
                }
                Array.Sort(renderedFrames);
                Array.Sort(terrainTimes);
                Array.Sort(contentTimes);
                var report = new RunReport
                {
                    seed = 431, speedMultiplier = multiplier, chunks = ChunkCount, fixedStepSeconds = 1f / 60f,
                    warmupChunks = WarmupChunks, steps = steps, contentEnabled = contentEnabled,
                    peakLoaded = peakLoaded, loadedBound = streamer.MaximumLoadedChunkCount,
                    warmedPoolHits = hits, warmedPoolRequests = requests, viewsCreatedAfterWarmup = final.Created - initial.Created,
                    missedRequiredWindows = missedEdges, missingHashes = missingHashes, differentHashes = differentHashes,
                    terrainAllocationEvents = terrainAllocationEvents, contentAllocationEvents = contentAllocationEvents,
                    maximumTerrainAllocationEventsPerStep = maximumTerrainAllocationEvents, maximumContentAllocationEventsPerStep = maximumContentAllocationEvents,
                    terrainP95Ms = Milliseconds(terrainTimes[steps * 95 / 100]), terrainMaxMs = Milliseconds(terrainTimes[steps - 1]),
                    contentP95Ms = Milliseconds(contentTimes[steps * 95 / 100]), contentMaxMs = Milliseconds(contentTimes[steps - 1]),
                    observedHarnessFrameMaxMs = maximumObservedFrameMs,
                    allocationCalibrationEvents = calibrationEvents,
                    maximumWholeFrameBytesIncludingHarness = maximumWholeFrameBytes,
                    renderedFrameSamples = renderedFrames.Length,
                    renderedFrameP95MsIncludingRunner = renderedFrames[renderedFrames.Length * 95 / 100],
                    renderedFrameMaxMsIncludingRunner = renderedFrames[renderedFrames.Length - 1],
                    environment = Application.unityVersion + "; " + Application.platform + "; " + SystemInfo.operatingSystem + "; " + SystemInfo.processorType + "; " + SystemInfo.graphicsDeviceName + "; " + Screen.width + "x" + Screen.height + "; Editor Mono; profiler=" + UnityEngine.Profiling.Profiler.enabled + "; vSync=" + QualitySettings.vSyncCount + "; targetFps=" + Application.targetFrameRate
                };
                Directory.CreateDirectory("Temp/streaming-acceptance");
                File.WriteAllText("Temp/streaming-acceptance/" + (contentEnabled ? "content-" : "terrain-") + multiplier + "x.json", JsonUtility.ToJson(report, true));
                Assert.AreEqual(0, missedEdges, "Required lookahead window had an unbuilt cell.");
                Assert.LessOrEqual(peakLoaded, streamer.MaximumLoadedChunkCount);
                Assert.Greater(requests, 0, "Stress run did not stream any surface chunks.");
                Assert.GreaterOrEqual(hits / (float)requests, 0.95f);
                Assert.AreEqual(0, final.Created - initial.Created, "Pool created views after warm-up.");
                Assert.AreEqual(0, missingHashes);
                Assert.AreEqual(0, differentHashes, "Speed changed streamed content.");
                Assert.AreEqual(ChunkCount - 1, lastRecorded);
                Assert.IsFalse(streamer.AllocationProbeOverflowed, "Allocation recorder capacity was exceeded; measurements are invalid.");
                if (_content != null) { _content.Dispose(); _content = null; }
                Object.Destroy(_root);
                Object.Destroy(_settings);
                yield return null;
            }
        }

        static double Milliseconds(long ticks) { return ticks * 1000d / Stopwatch.Frequency; }

        [Serializable]
        sealed class RunReport
        {
            [SerializeField] internal int seed, speedMultiplier, chunks, warmupChunks, steps, peakLoaded, loadedBound;
            [SerializeField] internal int warmedPoolHits, warmedPoolRequests, viewsCreatedAfterWarmup, missedRequiredWindows, missingHashes, differentHashes;
            [SerializeField] internal float fixedStepSeconds, observedHarnessFrameMaxMs;
            [SerializeField] internal bool contentEnabled;
            [SerializeField] internal long terrainAllocationEvents, contentAllocationEvents, maximumTerrainAllocationEventsPerStep, maximumContentAllocationEventsPerStep;
            [SerializeField] internal long allocationCalibrationEvents, maximumWholeFrameBytesIncludingHarness;
            [SerializeField] internal double terrainP95Ms, terrainMaxMs, contentP95Ms, contentMaxMs;
            [SerializeField] internal string environment;
            [SerializeField] internal int renderedFrameSamples;
            [SerializeField] internal float renderedFrameP95MsIncludingRunner, renderedFrameMaxMsIncludingRunner;
        }
    }
}
#endif
