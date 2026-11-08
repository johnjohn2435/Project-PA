using System.Linq;
using NUnit.Framework;
using PocketArcade.Meta;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.Tests
{
    public sealed class HubCarouselTests
    {
        private GameObject host;
        private ResourceManager resources;
        private GameCatalog catalog;
        static T Ref<T>(Component c, string field) where T : Object => (T)new SerializedObject(c).FindProperty(field).objectReferenceValue;
        [SetUp] public void Setup()
        {
            L10n.Initialize(Resources.Load<TextAsset>("Data/StringTable").text);
            resources=new GameObject("Carousel resources").AddComponent<ResourceManager>();
            resources.Initialize(path=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/"+path+".prefab"));
            catalog=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset"));
        }
        [TearDown] public void Cleanup()
        {
            if(host!=null)Object.DestroyImmediate(host);
            Object.DestroyImmediate(catalog);
            resources.Shutdown();Object.DestroyImmediate(resources.gameObject);L10n.Reset();
        }
        private HubView Bind(MiniGameDefinition[] games)
        {
            var data=new SerializedObject(catalog);var array=data.FindProperty("miniGames");array.arraySize=games.Length;
            for(int i=0;i<games.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=games[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            host=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/UI/Screens/Hub.prefab"));host.SetActive(false);
            ((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
            ((RectTransform)host.transform).ForceUpdateRectTransforms();
            var hub=host.GetComponent<HubView>();hub.Bind(catalog);hub.Present(catalog,"char_cat",0,new Core.Save.PlayerSettingsData(),_=>true);return hub;
        }
        [TestCase(0)] [TestCase(1)]
        public void EmptyAndSingleGameListsHaveNoDeadEndNavigation(int count)
        {
            var hub=Bind(catalog.MiniGames.Take(count).ToArray());var medals=Ref<HubMedals>(hub,"medals");
            Assert.AreEqual(count,medals.Count);
            Assert.IsFalse(Ref<Button>(medals,"previous").interactable);Assert.IsFalse(Ref<Button>(medals,"next").interactable);
            Assert.AreEqual(count>0,Ref<Button>(hub,"playButton").interactable);
        }
        [Test]
        public void CatalogExpansionReorderingAndLockedGamesWorkWithoutEditingHub()
        {
            var extra=new[]{"coming_soon_1","coming_soon_2","udada_delivery"}.Select(id=>AssetDatabase.LoadAssetAtPath<MiniGameDefinition>("Assets/_Project/ScriptableObjects/MiniGames/"+id+".asset"));
            var games=catalog.MiniGames.Reverse().Concat(extra).ToArray();var hub=Bind(games);var medals=Ref<HubMedals>(hub,"medals");
            var cards=medals.GetComponentsInChildren<GameMedalCardView>(true);
            Assert.AreEqual(6,medals.Count);CollectionAssert.AreEqual(games.Select(g=>g.Id),cards.Select(c=>c.GameId));
            var scroll=Ref<ScrollRect>(medals,"scroll");var previous=Ref<Button>(medals,"previous");var next=Ref<Button>(medals,"next");
            Assert.Greater(scroll.content.rect.width,scroll.viewport.rect.width*2);
            string initial=Ref<TMP_Text>(medals,"startLabel").text;string opened=null;hub.GameSelected+=id=>opened=id;
            for(int i=0;i<10;i++)next.onClick.Invoke();
            Assert.That(scroll.horizontalNormalizedPosition,Is.EqualTo(1).Within(.001));Assert.IsFalse(next.interactable);Assert.IsTrue(previous.interactable);
            Assert.AreEqual(initial,Ref<TMP_Text>(medals,"startLabel").text);Assert.IsNull(opened);
            var locked=cards.First(c=>c.GameId=="coming_soon_1");Assert.IsFalse(Ref<Button>(locked,"button").interactable);
            Ref<Button>(locked,"button").onClick.Invoke();Assert.AreEqual(initial,Ref<TMP_Text>(medals,"startLabel").text);
            Ref<Button>(cards.Last(),"button").onClick.Invoke();Assert.IsNull(opened);
            Ref<Button>(hub,"playButton").onClick.Invoke();Assert.AreEqual("udada_delivery",opened);
            L10n.SetLanguage(GameLanguage.English);medals.Present();Assert.AreEqual(games.Last().DisplayName,Ref<TMP_Text>(cards.Last(),"label").text);
            for(int i=0;i<10;i++)previous.onClick.Invoke();
            Assert.That(scroll.horizontalNormalizedPosition,Is.EqualTo(0).Within(.001));Assert.IsFalse(previous.interactable);
            var contentRect=scroll.content.rect;medals.Bind(catalog);Assert.AreEqual(6,medals.Count);Assert.AreEqual(contentRect,scroll.content.rect);
        }
    }
}
