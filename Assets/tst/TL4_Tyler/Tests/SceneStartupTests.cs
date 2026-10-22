using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s;
using EscapeThe90s.HUD;
using EscapeThe90s.GameStates;
public class SceneStartupTests
{
 GameObject manager, publisher, panel;
 [UnityTearDown] public IEnumerator Cleanup(){Object.Destroy(panel);Object.Destroy(publisher);Object.Destroy(manager);yield return null;}
 [UnityTest] public IEnumerator MenuStartsAndPublisherReceivesExactlyOneEventAfterReenable()
 {
  manager=new GameObject("GameStateManager");manager.AddComponent<GameStateManager>();
  publisher=new GameObject("Publisher");var events=publisher.AddComponent<GameStateEventPublisher>();
  panel=new GameObject("Menu");var menu=panel.AddComponent<MainMenuPanel>();
  int count=0;events.OnGameStarted+=()=>count++;
  publisher.SetActive(false);publisher.SetActive(true);
  yield return null;
  menu.StartGame();Assert.IsInstanceOf<PlayingState>(GameStateManager.Instance.CurrentState);Assert.AreEqual(1,count);
 }
 [UnityTest] public IEnumerator PublisherCanEnableBeforeManagerAndDisableAfterManagerDestruction()
 {
  publisher=new GameObject("Publisher");var events=publisher.AddComponent<GameStateEventPublisher>();
  manager=new GameObject("GameStateManager");manager.AddComponent<GameStateManager>();
  int count=0;events.OnGameStarted+=()=>count++;
  yield return null;
  GameStateManager.Instance.RequestStart();Assert.AreEqual(1,count);
  Object.Destroy(manager);yield return null;
  Assert.IsNull(GameStateManager.Instance);publisher.SetActive(false);
 }
 [UnityTest] public IEnumerator UnassignedPublisherCanDisableBeforeStartup()
 {
  publisher=new GameObject("Publisher");publisher.AddComponent<GameStateEventPublisher>();publisher.SetActive(false);
  yield return null;
 }
}
