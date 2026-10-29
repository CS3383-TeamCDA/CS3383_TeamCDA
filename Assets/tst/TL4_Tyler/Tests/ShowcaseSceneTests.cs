#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
public class ShowcaseSceneTests
{
 [UnityTest] public IEnumerator EveryAnimationPreviewsAndStatsScale()
 {
  EditorSceneManager.LoadSceneInPlayMode("Assets/tst/TL4_Tyler/Tests/TL4EnemyMovementTest.unity",new LoadSceneParameters(LoadSceneMode.Single));
  yield return null;
  var showcase=Object.FindAnyObjectByType<EnemyAnimationShowcase>();Assert.IsNotNull(showcase);Assert.AreEqual(3,showcase.PreviewCount);
  showcase.SetPreviewLevel(3);
  foreach(var enemy in showcase.GetComponentsInChildren<EnemyBase>()){Assert.AreEqual(140,enemy.MaxHealth);Assert.AreEqual(9,enemy.CoinReward);Assert.AreEqual(12,enemy.RangedDamage);}
  for(int i=0;i<3;i++)showcase.NextAnimation(i);
  yield return null;
  for(int step=0;step<7;step++){
   for(int i=0;i<3;i++)showcase.NextAnimation(i);
   yield return null;
   foreach(var enemy in showcase.GetComponentsInChildren<EnemyBase>())Assert.IsNotNull(enemy.GetComponent<SpriteRenderer>().sprite);
  }
  yield return null;
  var scene=showcase.gameObject.scene;
  SceneManager.CreateScene("AfterPreview");
  yield return SceneManager.UnloadSceneAsync(scene);
 }
}
#endif
