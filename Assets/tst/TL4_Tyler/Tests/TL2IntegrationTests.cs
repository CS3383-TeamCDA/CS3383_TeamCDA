using NUnit.Framework;
using UnityEngine;
using System.Reflection;
public class TL2IntegrationTests
{
 [Test] public void GeneratedChunksSupplyMarkersAndDifficultyToSpawner()
 {
  var root=new GameObject("Integration");root.SetActive(false);
  var template=new GameObject("Room template");
  var marker=new GameObject("Marker");marker.transform.SetParent(template.transform);marker.AddComponent<EnemySpawnPoint>();
  var view=new GameObject("Camera");var camera=view.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.aspect=1;view.transform.position=new Vector3(0,0,-10);
  try
  {
   var generator=root.AddComponent<LevelGenerator>();
   foreach(string field in new[]{"StartChunkGen","MiddleChunkGen","EndChunkGen"})
   {
    var chunks=root.AddComponent<ChunkSpawner>();
    Set(chunks,"chunkPrefab",template);Set(chunks,"parentTransform",root.transform);
    Set(generator,field,chunks);
   }
   // TL4 owns difficulty and marker registration; TL2 only generates rooms.
   var population=root.AddComponent<EnemyLevelPopulation>();
   population.SetDifficultyLevel(3);
   root.SetActive(true);generator.GenerateLevel();
   population.RefreshSpawnPoints();
   var spawner=root.GetComponent<EnemySpawner>();spawner.ConfigureCamera(camera);
   Assert.AreEqual(3,root.GetComponentsInChildren<EnemySpawnPoint>().Length);
   EnemyBase spawned=null;spawner.OnEnemySpawned+=e=>spawned=e;spawner.Tick(1);
   Assert.IsNotNull(spawned);Assert.AreEqual(3,spawned.Level);Assert.AreEqual(140,spawned.MaxHealth);
   Assert.AreEqual(16,spawned.transform.position.x);Assert.AreEqual(9,spawned.CoinReward);
  }
  finally
  {
   while(root.transform.childCount>0)Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
   Object.DestroyImmediate(root);Object.DestroyImmediate(template);Object.DestroyImmediate(view);
  }
 }
 static void Set(object owner,string name,object value){owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);}
}
