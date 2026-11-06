using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class ChunkGenerationTests
    {
        [Test, Category("M6")]
        public void SameSeedAndInputs_ProduceIdenticalChunkMetadata()
        {
            BiomeRules[] biomes = DeterminismChecks.TestBiomes();
            StreetRequest request = DeterminismChecks.Req(7, 11, 8, 2, BuildingFlags.All, 2);
            request.Kind = StreetKind.Avenue;
            request.Difficulty = 2.5f;

            StreetLayout first = StreetGenerator.Generate(2401, request, biomes[2]);
            StreetLayout second = StreetGenerator.Generate(2401, request, biomes[2]);

            Assert.AreEqual(first.GenerationHash(), second.GenerationHash());
            Assert.AreEqual(first.ChunkCount, first.ChunkSpecs.Length);
            foreach (ChunkSpec spec in first.ChunkSpecs)
            {
                Assert.That(spec.Stamp.StartCell, Is.GreaterThanOrEqualTo(0));
                Assert.That(spec.Stamp.StartCell + spec.Stamp.WidthCells, Is.LessThanOrEqualTo(WorldConstants.ChunkWidthCells));
                Assert.AreEqual(spec.Content.Enemies.Length, spec.SpawnLocations.Count);
            }
        }

        [Test, Category("M6")]
        public void SafeAndBuildingChunks_ReceiveFlatGroundStamps()
        {
            BiomeRules[] biomes = DeterminismChecks.TestBiomes();
            StreetRequest request = DeterminismChecks.Req(2, 4, 8, 0, BuildingFlags.All, 0,
                BuildingType.Arcade);
            StreetLayout layout = StreetGenerator.Generate(391, request, biomes[0]);

            for (int i = 0; i < layout.ChunkSpecs.Length; i++)
            {
                ChunkData chunk = layout.Chunks[i];
                if (chunk.IsSafe || chunk.Building != BuildingType.None)
                    Assert.AreEqual(TerrainStampKind.Flat, layout.ChunkSpecs[i].Stamp.Kind);
            }
        }
    }
}
