using System.Collections;
using NUnit.Framework;
using PocketArcade.EchoRoom;
using PocketArcade.EchoRoom.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace PocketArcade.Tests.EchoPlay
{
    public sealed class EchoPlayTests
    {
        private ResourceManager resources;private UIManager ui;private GameObject canvas;private EchoHud hud;private Transform live;
        private static RectTransform Rect(string n,Transform parent){var r=(RectTransform)new GameObject(n,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        [UnitySetUp] public IEnumerator Setup()
        {
            L10n.Initialize(Resources.Load<TextAsset>("Data/StringTable").text);resources=new GameObject("TestResources").AddComponent<ResourceManager>();resources.Initialize();
            yield return resources.Preload(new[]{new BootResourceSlot{path=BootResourceManifest.CharacterPrefab("char_cat"),kind="Prefab",required=true},new BootResourceSlot{path=BootResourceManifest.CharacterPrefab("char_dog"),kind="Prefab",required=true},new BootResourceSlot{path=BootResourceManifest.CharacterPrefab("char_hamster"),kind="Prefab",required=true},new BootResourceSlot{path=EchoHud.WorldPath,kind="Prefab",required=true},new BootResourceSlot{path=EchoHud.ScreenKey,kind="Prefab",required=true},new BootResourceSlot{path=EchoHud.MenuKey,kind="Prefab",required=true}});Assert.That(resources.PreloadError,Is.Null);
            ui=new GameObject("TestUI").AddComponent<UIManager>();ui.Initialize();canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);
            live=Rect("Live",canvas.transform);var inactive=Rect("Inactive",canvas.transform);inactive.gameObject.SetActive(false);var task=ui.Mount(live,inactive,EchoHud.ScreenKey,EchoHud.ScreenKey);while(!task.IsCompleted)yield return null;task.GetAwaiter().GetResult();hud=(EchoHud)ui.Find(EchoHud.ScreenKey);hud.SendMessage("OnApplicationFocus",true);hud.SendMessage("OnApplicationPause",false);yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(ui!=null){ui.Shutdown();Object.Destroy(ui.gameObject);}if(resources!=null){resources.Shutdown();Object.Destroy(resources.gameObject);}if(canvas!=null)Object.Destroy(canvas);yield return null;L10n.Reset();}
        [UnityTest] public IEnumerator RecordedRouteReplaysAndDeliversThroughPrefabScreen()
        {
            hud.Play();hud.Session.Tick(.7,new EchoPoint(0,1));hud.Session.Tick(.7,new EchoPoint(0,-1));Assert.That(hud.Session.Delivered,Is.EqualTo(1));hud.Session.Tick(8.6,new EchoPoint(0,0));hud.Session.Tick(1.4,new EchoPoint(0,0));yield return null;
            Assert.That(hud.Session.GhostCount,Is.EqualTo(1));Assert.That(hud.Session.Delivered,Is.EqualTo(2));Assert.That(hud.Session.GhostDeliveries,Is.EqualTo(1));Assert.That(Object.FindFirstObjectByType<EchoWorldView>().GetComponentsInChildren<PocketArcade.Characters.CharacterRigController>().Length,Is.EqualTo(6));
        }
        [UnityTest] public IEnumerator PauseFreezesClockGhostPlaybackAndAnimation()
        {
            hud.Play();hud.Session.Tick(10,new EchoPoint(0,0));hud.Pause();yield return null;yield return null;double elapsed=hud.Session.Elapsed;var position=hud.Session.ActorAt(1).Position;
            yield return null;yield return null;Assert.That(hud.Session.Elapsed,Is.EqualTo(elapsed));Assert.That(hud.Session.ActorAt(1).Position.X,Is.EqualTo(position.X));
            foreach(var rig in Object.FindFirstObjectByType<EchoWorldView>().GetComponentsInChildren<PocketArcade.Characters.CharacterRigController>())Assert.That(rig.GetComponent<Animator>().speed,Is.Zero);
        }
        [UnityTest] public IEnumerator RetryClearsTracesAndDoesNotSpawnExtraWorld()
        {
            hud.Play();hud.Session.Tick(20,new EchoPoint(0,0));hud.Pause();yield return null;yield return null;var previous=hud.Session;
            ui.Find(EchoHud.MenuKey).transform.Find("Card/Retry").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Assert.That(hud.Session,Is.Not.SameAs(previous));Assert.That(hud.Session.GhostCount,Is.Zero);Assert.That(hud.Session.Delivered,Is.Zero);Assert.That(Object.FindObjectsByType<EchoWorldView>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator FocusLossPausesAndUnmountReleasesAllCharacterInstances()
        {
            hud.Play();hud.SendMessage("OnApplicationFocus",false);yield return null;yield return null;hud.SendMessage("OnApplicationFocus",true);double elapsed=hud.Session.Elapsed;yield return null;Assert.That(hud.Session.Elapsed,Is.EqualTo(elapsed));ui.Unmount(live);yield return null;
            Assert.That(Object.FindObjectsByType<EchoWorldView>(FindObjectsSortMode.None).Length,Is.Zero);Assert.That(Object.FindObjectsByType<PocketArcade.Characters.CharacterRigController>(FindObjectsSortMode.None).Length,Is.Zero);
        }
        [UnityTest] public IEnumerator UnspawnedGhostsHideEveryRendererIncludingSkinnedBodies()
        {
            var rigs=Object.FindFirstObjectByType<EchoWorldView>().GetComponentsInChildren<PocketArcade.Characters.CharacterRigController>(true);
            Assert.That(rigs[0].GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,Is.GreaterThan(0));
            for(int i=1;i<rigs.Length;i++)foreach(var renderer in rigs[i].GetComponentsInChildren<Renderer>(true))Assert.That(renderer.enabled,Is.False,renderer.name);
            hud.Play();hud.Session.Tick(10,new EchoPoint(0,0));yield return null;
            foreach(var renderer in rigs[1].GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Assert.That(renderer.enabled,Is.True);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,0);
                Assert.That(block.GetColor("_Color").a,Is.InRange(.01f,.6f));Assert.That(renderer.sharedMaterial.GetColor("_Color").a,Is.GreaterThan(.9f));
            }
            for(int i=2;i<rigs.Length;i++)foreach(var renderer in rigs[i].GetComponentsInChildren<Renderer>(true))Assert.That(renderer.enabled,Is.False,renderer.name);
        }
        [UnityTest] public IEnumerator RestartHidesRecordedSkinnedGhostsAgain()
        {
            hud.Play();hud.Session.Tick(30,new EchoPoint(0,0));yield return null;hud.Reset();yield return null;
            var rigs=Object.FindFirstObjectByType<EchoWorldView>().GetComponentsInChildren<PocketArcade.Characters.CharacterRigController>(true);
            Assert.That(hud.Session.GhostCount,Is.Zero);
            for(int i=1;i<rigs.Length;i++)foreach(var renderer in rigs[i].GetComponentsInChildren<Renderer>(true))Assert.That(renderer.enabled,Is.False,renderer.name);
            Assert.That(rigs[0].transform.Find("body").GetComponent<SpriteRenderer>().enabled,Is.False);
        }
        [UnityTest] public IEnumerator EverySelectedSkinHidesUnusedGhostsAndKeepsTheirBodyWithTheirHead()
        {
            foreach(var id in new[]{"char_cat","char_dog","char_hamster"})
            {
                var instance=resources.Spawn(EchoHud.WorldPath,null);var view=instance.GetComponent<EchoWorldView>();
                try
                {
                    view.Attach(resources,id);var session=new EchoSession();view.Present(session);
                    var rigs=view.GetComponentsInChildren<PocketArcade.Characters.CharacterRigController>(true);
                    for(int i=1;i<rigs.Length;i++)foreach(var renderer in rigs[i].GetComponentsInChildren<Renderer>(true))Assert.That(renderer.enabled,Is.False,id+" "+renderer.name);
                    session.Start();session.Tick(.5,new EchoPoint(0,1));view.Present(session);yield return null;
                    foreach(var renderer in rigs[0].GetComponentsInChildren<Renderer>(true))if(renderer.enabled)Assert.That(Mathf.Abs(renderer.bounds.center.y-rigs[0].transform.position.y),Is.LessThan(1.5f),id+" "+renderer.name);
                    session.Tick(9.5,new EchoPoint(0,0));view.Present(session);yield return null;
                    for(int i=2;i<rigs.Length;i++)foreach(var renderer in rigs[i].GetComponentsInChildren<Renderer>(true))Assert.That(renderer.enabled,Is.False,id+" "+renderer.name);
                }
                finally{view.Release(resources);resources.Release(instance);}
                yield return null;
            }
        }
    }
}
