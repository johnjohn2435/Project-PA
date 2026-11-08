using System.Collections;
using NUnit.Framework;
using PocketArcade.TiltFloor;
using PocketArcade.TiltFloor.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace PocketArcade.Tests.FloorPlay
{
    public sealed class FloorPlayTests
    {
        private ResourceManager resources;private UIManager ui;private GameObject canvas;private FloorHud hud;private Transform live;
        private static RectTransform Rect(string name,Transform parent){var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        [UnitySetUp] public IEnumerator Setup()
        {
            L10n.Initialize(Resources.Load<TextAsset>("Data/StringTable").text);
            resources=new GameObject("TestResources").AddComponent<ResourceManager>();resources.Initialize();
            yield return resources.Preload(new[]{new BootResourceSlot{path=BootResourceManifest.CharacterPrefab("char_cat"),kind="Prefab",required=true},new BootResourceSlot{path=FloorHud.WorldPath,kind="Prefab",required=true},new BootResourceSlot{path=FloorHud.ScreenKey,kind="Prefab",required=true},new BootResourceSlot{path=FloorHud.MenuKey,kind="Prefab",required=true}});Assert.That(resources.PreloadError,Is.Null);
            ui=new GameObject("TestUI").AddComponent<UIManager>();ui.Initialize();canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scale=canvas.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1080,1920);
            live=Rect("Live",canvas.transform);var inactive=Rect("Inactive",canvas.transform);inactive.gameObject.SetActive(false);
            var task=ui.Mount(live,inactive,FloorHud.ScreenKey,FloorHud.ScreenKey);while(!task.IsCompleted)yield return null;task.GetAwaiter().GetResult();
            hud=(FloorHud)ui.Find(FloorHud.ScreenKey);hud.SendMessage("OnApplicationFocus",true);hud.SendMessage("OnApplicationPause",false);yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(ui!=null){ui.Shutdown();Object.Destroy(ui.gameObject);}if(resources!=null){resources.Shutdown();Object.Destroy(resources.gameObject);}if(canvas!=null)Object.Destroy(canvas);yield return null;L10n.Reset();}
        [UnityTest] public IEnumerator DragMovesCharacterAndPauseCancelsInput()
        {
            hud.Play();var input=hud.GetComponentInChildren<FloorInput>(true);var e=new PointerEventData(null){pointerId=42,position=new Vector2(100,100)};input.OnPointerDown(e);e.position=new Vector2(1000,100);input.OnDrag(e);
            yield return null;
            var rig = Object.FindFirstObjectByType<FloorWorldView>().GetComponentInChildren<PocketArcade.Characters.CharacterRigController>();
            Assert.That(rig.GetComponent<Animator>().GetFloat("Speed"), Is.GreaterThan(0));
            Assert.That(input.Move.x,Is.GreaterThan(.9f));hud.Session.Tick(.3,new FloorVector(input.Move.x,input.Move.y));Assert.That(hud.Session.Player.X,Is.GreaterThan(0));hud.Pause();yield return null;yield return null;
            Assert.That(input.Move.magnitude,Is.LessThan(.01f));double elapsed=hud.Session.Elapsed;double tilt=hud.Session.Tilt.X;var body=hud.Session.BodyAt(0).Position;
            yield return null;yield return null;Assert.That(hud.Session.Elapsed,Is.EqualTo(elapsed));Assert.That(hud.Session.Tilt.X,Is.EqualTo(tilt));Assert.That(hud.Session.BodyAt(0).Position.X,Is.EqualTo(body.X));
        }
        [UnityTest] public IEnumerator SecondaryPointerCannotStealJoystick()
        {
            var input=hud.GetComponentInChildren<FloorInput>(true);var first=new PointerEventData(null){pointerId=1,position=Vector2.zero};var second=new PointerEventData(null){pointerId=2,position=Vector2.zero};input.OnPointerDown(first);input.OnPointerDown(second);second.position=new Vector2(1000,1000);input.OnDrag(second);Assert.That(input.Move.magnitude,Is.LessThan(.01f));first.position=new Vector2(1000,0);input.OnDrag(first);input.OnPointerUp(second);Assert.That(input.Move.x,Is.GreaterThan(.9f));input.OnPointerUp(first);Assert.That(input.Move.magnitude,Is.LessThan(.01f));yield return null;
        }
        [UnityTest] public IEnumerator RetryResetsBoardWithoutSpawningExtraWorld()
        {
            hud.Play();hud.Session.Tick(.5,new FloorVector(1,0));var old=hud.Session;hud.Pause();yield return null;yield return null;
            ui.Find(FloorHud.MenuKey).transform.Find("Card/Retry").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Assert.That(hud.Session,Is.Not.SameAs(old));Assert.That(hud.Session.Score,Is.LessThan(3));Assert.That(hud.Session.Player.Length,Is.LessThan(.1));Assert.That(Object.FindObjectsByType<FloorWorldView>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator FocusLossKeepsPauseUntilResumeAndUnmountReleasesWorld()
        {
            hud.Play();hud.SendMessage("OnApplicationFocus",false);yield return null;yield return null;hud.SendMessage("OnApplicationFocus",true);double time=hud.Session.Elapsed;yield return null;Assert.That(hud.Session.Elapsed,Is.EqualTo(time));Assert.That(ui.Find(FloorHud.MenuKey).IsOpen,Is.True);
            ui.Unmount(live);yield return null;Assert.That(Object.FindObjectsByType<FloorWorldView>(FindObjectsSortMode.None).Length,Is.Zero);
        }
        [UnityTest] public IEnumerator LandingShakesCameraThenRestoresAndRetryClearsImpact()
        {
            hud.Pause();yield return null;yield return null;
            var view=Object.FindFirstObjectByType<FloorWorldView>();
            var s=new FloorSession(new FloorRules{FirstDrop=.05,SpawnInterval=100,WarningSeconds=.4,DropSeconds=.4,SpawnRadiusRatio=0,FirstBonus=100});
            view.Present(s);var rest=view.PlayCamera.transform.position;
            s.Start();s.Tick(1,new FloorVector(0,0));view.Present(s);
            Assert.That(s.Landed,Is.EqualTo(1));Assert.That(Vector3.Distance(rest,view.PlayCamera.transform.position),Is.GreaterThan(.01f));
            float deadline=Time.realtimeSinceStartup+.5f; while(Time.realtimeSinceStartup<deadline){yield return null;view.Present(s);}
            Assert.That(Vector3.Distance(rest,view.PlayCamera.transform.position),Is.LessThan(.0001f));
            view.Present(new FloorSession());Assert.That(Vector3.Distance(rest,view.PlayCamera.transform.position),Is.LessThan(.0001f));
        }
        [UnityTest] public IEnumerator EnemyContactShowsDeathAndRetryRestartsCharacter()
        {
            var session=new FloorSession(new FloorRules{FirstDrop=.05,SpawnInterval=100,WarningSeconds=.4,DropSeconds=.4,SpawnRadiusRatio=0,FirstBonus=100});
            typeof(FloorHud).GetProperty("Session").SetValue(hud,session);session.Start();session.Tick(2,new FloorVector(0,0));yield return null;
            Assert.That(hud.Session.State,Is.EqualTo(FloorState.Hit));
            Assert.That(hud.transform.Find("Status").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo(L10n.Format("floor.hit_over",session.Score,session.Elapsed,session.BonusScore)));
            var rig=Object.FindFirstObjectByType<FloorWorldView>().GetComponentInChildren<PocketArcade.Characters.CharacterRigController>();Assert.That(rig.GetComponent<Animator>().speed,Is.Zero);
            var retry=hud.transform.Find("Primary").GetComponent<Button>();Assert.That(retry.interactable,Is.True);retry.onClick.Invoke();yield return null;
            Assert.That(hud.Session,Is.Not.SameAs(session));Assert.That(hud.Session.State,Is.EqualTo(FloorState.Playing));Assert.That(rig.GetComponent<Animator>().speed,Is.EqualTo(1));
        }
    }
}
