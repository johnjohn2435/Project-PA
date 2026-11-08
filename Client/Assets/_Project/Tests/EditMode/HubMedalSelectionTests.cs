using System.Linq;
using NUnit.Framework;
using PocketArcade.Meta;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.Tests
{
    // Replaces the previous inline-medal contract with the walking hub contract.
    public sealed class HubParkTests
    {
        ResourceManager resources;
        GameObject host;
        HubParkController controller;
        static T Ref<T>(Component c,string name) where T:Object => (T)new SerializedObject(c).FindProperty(name).objectReferenceValue;
        [SetUp] public void Setup()
        {
            L10n.Initialize(Resources.Load<TextAsset>("Data/StringTable").text);
            resources=new GameObject("Park test resources").AddComponent<ResourceManager>();
            resources.Initialize(path=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/"+path+".prefab"));
            typeof(HubParkController).GetMethod("ResetVisit",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
        }
        [TearDown] public void Cleanup()
        {
            controller?.Dispose();controller=null;
            if(host!=null)Object.DestroyImmediate(host);
            resources.Shutdown();Object.DestroyImmediate(resources.gameObject);L10n.Reset();
        }
        [Test] public void EntranceRequiresProximityAndExplicitActionAndRejectsBlockedInput()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");
            string opened=null;controller=new HubParkController(resources,catalog,id=>opened=id);
            controller.Step(Vector2.zero,true,0);controller.Enter();Assert.IsNull(opened);
            foreach(var gate in controller.World.Entrances)
            {
                controller.World.Present(gate.position,Vector2.zero);
                controller.Step(Vector2.zero,true,0);
                Assert.AreEqual(gate.gameId,controller.NearbyGameId);Assert.IsNull(opened);
                controller.Step(Vector2.zero,false,0);controller.Enter();Assert.IsNull(opened);
                controller.Step(Vector2.zero,true,0);controller.Enter();Assert.AreEqual(gate.gameId,opened);opened=null;
            }
        }
        [Test] public void WalkingCannotTunnelThroughBuildingsAndSlidesAlongWalls()
        {
            var bounds=new Rect(-5,-5,10,10);var obstacles=new[]{new Rect(0,-1,1,2)};
            var p=HubParkNavigation.Move(new Vector2(-1,0),new Vector2(8,0),bounds,obstacles);
            Assert.Less(p.x,0);
            p=HubParkNavigation.Move(new Vector2(-.05f,0),new Vector2(1,.8f),bounds,obstacles);
            Assert.Less(p.x,0);Assert.Greater(p.y,.7f);
            p=HubParkNavigation.Move(new Vector2(-3,3),new Vector2(-9,9),bounds,obstacles);
            Assert.AreEqual(new Vector2(-5,5),p);
        }
        [Test] public void RealParkHasReachableEntrancesForEveryAvailableGame()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");
            var world=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/Hub/ParkWorld.prefab").GetComponent<HubParkWorld>();
            CollectionAssert.AreEquivalent(catalog.MiniGames.Where(g=>g.IsAvailable).Select(g=>g.Id),world.Entrances.Select(g=>g.gameId));
            foreach(var gate in world.Entrances)
            {Assert.IsTrue(world.WalkArea.Contains(gate.position));Assert.IsFalse(world.Obstacles.Any(r=>r.Contains(gate.position)));}
        }
        [Test] public void RosterExpandsBeyondThreeFriendsAndHonorsSelectionAndLocks()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");
            var catalog=Object.Instantiate(source);
            var data=new SerializedObject(catalog);var items=data.FindProperty("characters");items.arraySize=12;
            for(int i=0;i<12;i++)items.GetArrayElementAtIndex(i).objectReferenceValue=source.Characters[i%source.Characters.Count];
            data.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                host=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/UI/Screens/Hub.prefab"));host.SetActive(false);
                var roster=host.GetComponentInChildren<CharacterRosterView>(true);
                string selected=null;roster.Bind(catalog,id=>selected=id);roster.Present("char_cat",id=>id=="char_cat");
                Assert.AreEqual(12,roster.Count);
                var content=Ref<RectTransform>(roster,"content");Assert.Greater(content.rect.height,2000);
                var cards=roster.GetComponentsInChildren<CharacterCardView>(true);
                Assert.IsFalse(Ref<Button>(cards[1],"button").interactable);
                Assert.IsTrue(Ref<Image>(cards[0],"selectedBadge").enabled);
                Ref<Button>(cards[0],"button").onClick.Invoke();Assert.AreEqual("char_cat",selected);
                L10n.SetLanguage(GameLanguage.English);roster.Present("char_cat",_=>true);
                Assert.AreEqual("12 friends",Ref<TMP_Text>(roster,"countLabel").text);
            }
            finally{Object.DestroyImmediate(catalog);}
        }
        [Test] public void GameMedalsWaitForStartAndKeepTheSeparateFriendSelector()
        {
            host=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/UI/Screens/Hub.prefab"));host.SetActive(false);
            var hub=host.GetComponent<HubView>();var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");
            hub.Bind(catalog);hub.Present(catalog,"char_cat",0,new Core.Save.PlayerSettingsData(),_=>true);
            var medals=Ref<HubMedals>(hub,"medals");var home=Ref<GameObject>(hub,"homePanel");
            Assert.IsNotNull(medals);Assert.IsNull(Ref<HubParkView>(hub,"park"));
            Assert.IsEmpty(host.GetComponentsInChildren<HubJoystick>(true));
            Assert.AreEqual(catalog.Characters.Count,Ref<CharacterRosterView>(hub,"roster").Count);
            string opened=null;hub.GameSelected+=id=>opened=id;
            foreach(var game in catalog.MiniGames.Where(game=>game.IsAvailable))
            {
                var button=medals.GetComponentsInChildren<Button>(true).First(b=>b.name==game.Id);
                Assert.IsTrue(button.gameObject.activeSelf);Assert.IsTrue(button.transform.IsChildOf(home.transform));
                button.onClick.Invoke();Assert.IsNull(opened);
                Ref<Button>(hub,"playButton").onClick.Invoke();Assert.AreEqual(game.Id,opened);opened=null;
            }
        }
    }
}
