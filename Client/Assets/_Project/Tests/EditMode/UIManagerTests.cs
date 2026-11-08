using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using PocketArcade.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace PocketArcade.Tests
{
    public sealed class FrameTestPanel : UIPanel
    {
        public int Opens,Shows,Hides,Closes;
        public object Data;
        protected override void OnOpen(object data){Opens++;Data=data;}
        protected override void OnShow()=>Shows++;
        protected override void OnHide()=>Hides++;
        protected override void OnClose()=>Closes++;
    }
    public sealed class FrameTestPopup : UIPopup
    {
        public bool Consume;
        protected override bool OnBackRequested()=>Consume;
    }
    [ExecuteAlways] public sealed class FrameEnableProbe : MonoBehaviour
    {
        public static int Enabled;
        private void OnEnable()=>Enabled++;
    }
    public sealed class FrameTestEventSystem : EventSystem
    {
        public void EnableForTest()=>base.OnEnable();
        public void DisableForTest()=>base.OnDisable();
    }
    public sealed class FailingFramePanel : UIPanel
    { protected override void OnOpen(object data)=>throw new InvalidOperationException("test-hook"); }
    public sealed class UIManagerTests
    {
        private readonly List<GameObject> roots=new List<GameObject>();
        private readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
        private UIManager ui;
        private Transform live,staging;
        private int loads;
        private GameObject Root(string name,bool active=true)
        {var go=new GameObject(name,typeof(RectTransform));go.SetActive(active);roots.Add(go);return go;}
        private T Prefab<T>(string key,Placement placement=Placement.Frame,OrderPolicy policy=OrderPolicy.Managed,int order=100)where T:UIBase
        {
            var go=Root(key,false);go.AddComponent<Canvas>().sortingOrder=order;go.AddComponent<CanvasGroup>();
            var window=go.AddComponent<T>();Set(window,"key",key);Set(window,"placement",(int)placement);Set(window,"orderPolicy",(int)policy);prefabs[key]=go;return window;
        }
        private static void Set(Object target,string field,object value)
        {
            var data=new SerializedObject(target);var p=data.FindProperty(field);
            if(value is string s)p.stringValue=s;else if(value is bool b)p.boolValue=b;else p.enumValueIndex=(int)value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static T Done<T>(Task<T> task)
        {Assert.That(task.IsCompleted,Is.True,"Synchronous boot-cache operations should finish without polling.");return task.GetAwaiter().GetResult();}
        private Task<GameObject> Load(string key){loads++;return Task.FromResult(prefabs[key]);}
        private void Mount(Func<string,Task<GameObject>> loader=null)=>Done(ui.Mount(live,staging,"root","root",loader??Load));
        [SetUp]public void Setup()
        {
            loads=0;
            L10n.Initialize(System.IO.File.ReadAllText(PocketArcade.Editor.StringTableSetup.TableFile));
            ui=Root("Manager").AddComponent<UIManager>();ui.Initialize();live=Root("Live").transform;staging=Root("Inactive",false).transform;
            Prefab<FrameTestPanel>("root");Prefab<FrameTestPanel>("next");Prefab<FrameTestPopup>("popup");Prefab<FrameTestPopup>("popup2");
        }
        [TearDown]public void Teardown()
        {ui.Shutdown();foreach(var root in roots)if(root!=null)Object.DestroyImmediate(root);roots.Clear();prefabs.Clear();L10n.Reset();}
        [Test]public void PanelFrameHidesAndRestoresItsPopupWithoutClosing()
        {
            Mount();var root=(FrameTestPanel)ui.Find("root");var popup=Done(ui.Open("popup"));
            var next=(FrameTestPanel)Done(ui.Open("next",42));
            Assert.That(root.IsOpen&&popup.IsOpen,Is.True);Assert.That(root.IsVisible||popup.IsVisible,Is.False);Assert.That(root.Closes,Is.Zero);
            Assert.That(next.Data,Is.EqualTo(42));Done(ui.Close("next"));
            Assert.That(root.IsVisible&&popup.IsVisible,Is.True);Assert.That(root.Opens,Is.EqualTo(1));Assert.That(root.Shows,Is.EqualTo(2));
            Assert.That(next.IsOpen||next.IsVisible,Is.False);
        }
        [Test]public void RootCannotCloseAndBackRaisesRootEvent()
        {Mount();int count=0;ui.RootBackRequested+=()=>count++;Assert.That(Done(ui.Close("root")),Is.False);Done(ui.Back());Assert.That(count,Is.EqualTo(1));Assert.That(ui.Find("root").IsOpen,Is.True);}
        [Test]public void FifoWaitsForAsyncLoadBeforeBackHomeAndNextOpen()
        {
            var held=new TaskCompletionSource<GameObject>();Mount(k=>k=="next"?held.Task:Load(k));
            var open=ui.Open("next");var popup=ui.Open("popup");var back=ui.Back();var home=ui.Home();
            Assert.That(open.IsCompleted||popup.IsCompleted||back.IsCompleted||home.IsCompleted,Is.False);
            Assert.That(ui.Find("root").IsVisible,Is.True);held.SetResult(prefabs["next"]);
            Done(open);Done(popup);Done(back);Done(home);
            Assert.That(ui.Frames.Count,Is.EqualTo(1));Assert.That(ui.Find("root").IsVisible,Is.True);Assert.That(ui.Find("popup").IsOpen,Is.False);
        }
        [Test]public void FailedLoadDoesNotPoisonFollowingCommands()
        {
            Mount(k=>k=="bad"?Task.FromException<GameObject>(new InvalidOperationException("test")):Load(k));
            Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("bad")));Done(ui.Open("next"));Assert.That(ui.Frames.Count,Is.EqualTo(2));
        }
        [Test]public void CloseCachesInstanceAndDuplicateOpenIsRejected()
        {
            Mount();var first=Done(ui.Open("popup"));Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("popup")));
            Done(ui.Close("popup"));var second=Done(ui.Open("popup"));Assert.That(second,Is.SameAs(first));Assert.That(loads,Is.EqualTo(2));
        }
        [TestCase(Placement.Frame,OrderPolicy.Fixed)]
        [TestCase(Placement.Independent,OrderPolicy.Managed)]
        public void InvalidPlacementOrderPairsAreRejected(Placement placement,OrderPolicy policy)
        {Mount();Prefab<FrameTestPopup>("invalid",placement,policy);Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("invalid")));Assert.That(ui.CachedCount,Is.EqualTo(1));}
        [Test]public void IndependentPanelIsRejected()
        {Mount();Prefab<FrameTestPanel>("invalid",Placement.Independent,OrderPolicy.Fixed);Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("invalid")));}
        [Test]public void NestedDuplicateRegistrationRollsBackEveryKey()
        {
            Mount();var parent=Prefab<FrameTestPanel>("bundle");var child=Prefab<FrameTestPopup>("root");child.transform.SetParent(parent.transform);
            Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("bundle")));Assert.That(ui.Find("bundle"),Is.Null);Assert.That(ui.CachedCount,Is.EqualTo(1));
            Assert.That(live.childCount,Is.EqualTo(1));Assert.That(staging.childCount,Is.Zero);
        }
        [Test]public void NestedValidDefinitionsAreRegisteredTogetherButOnlyRequestedUiOpens()
        {
            Mount();var parent=Prefab<FrameTestPanel>("bundle");var child=Prefab<FrameTestPopup>("child");child.transform.SetParent(parent.transform);
            Done(ui.Open("bundle"));Assert.That(ui.Find("child"),Is.Not.Null);Assert.That(ui.Find("child").IsOpen,Is.False);Done(ui.Open("child"));Assert.That(ui.Frames[1].Popups.Count,Is.EqualTo(1));
        }
        [Test]public void FixedOverlayConsumesBackBeforePopupAndPanelEvenWhenNotClosable()
        {
            var fixedWindow=Prefab<FrameTestPopup>("overlay",Placement.Independent,OrderPolicy.Fixed,500);Set(fixedWindow,"closeOnBack",false);
            Mount();Done(ui.Open("next"));var popup=Done(ui.Open("popup"));var overlay=Done(ui.Open("overlay"));
            Done(ui.Back());Assert.That(overlay.IsOpen&&popup.IsOpen,Is.True);Assert.That(ui.Frames.Count,Is.EqualTo(2));
            Done(ui.Close("overlay"));Done(ui.Back());Assert.That(popup.IsOpen,Is.False);Done(ui.Back());Assert.That(ui.Frames.Count,Is.EqualTo(1));
        }
        [Test]public void BackHookCanConsumeWithoutClosing()
        {Mount();var popup=(FrameTestPopup)Done(ui.Open("popup"));popup.Consume=true;Done(ui.Back());Assert.That(popup.IsOpen,Is.True);}
        [Test]public void SortingIsRecomputedInTensWhileFixedOrderStaysUnchanged()
        {
            Prefab<FrameTestPopup>("fixed",Placement.Independent,OrderPolicy.Fixed,700);Mount();var first=Done(ui.Open("popup"));var second=Done(ui.Open("popup2"));var overlay=Done(ui.Open("fixed"));
            Assert.That(ui.Find("root").Canvas.sortingOrder,Is.EqualTo(100));Assert.That(first.Canvas.sortingOrder,Is.EqualTo(110));Assert.That(second.Canvas.sortingOrder,Is.EqualTo(120));
            Done(ui.Close("popup"));Assert.That(second.Canvas.sortingOrder,Is.EqualTo(110));Assert.That(overlay.Canvas.sortingOrder,Is.EqualTo(700));
        }
        [Test]public void HomeClosesAllPopupsAndOverlaysButPreservesRootAndCache()
        {
            Prefab<FrameTestPopup>("fixed",Placement.Independent,OrderPolicy.Fixed);Mount();Done(ui.Open("popup"));Done(ui.Open("next"));Done(ui.Open("popup2"));Done(ui.Open("fixed"));Done(ui.Home());
            Assert.That(ui.Frames.Count,Is.EqualTo(1));Assert.That(ui.Frames[0].Popups.Count,Is.Zero);Assert.That(ui.Find("root").IsVisible,Is.True);Assert.That(ui.CachedCount,Is.EqualTo(5));Assert.That(ui.Find("fixed").IsOpen,Is.False);
        }
        [Test]public void ShutdownCancelsPendingLoadAndQueuedOperationsWithoutLateRegistration()
        {
            var held=new TaskCompletionSource<GameObject>();Mount(k=>k=="next"?held.Task:Load(k));var open=ui.Open("next");var back=ui.Back();
            ui.Shutdown();held.SetResult(prefabs["next"]);Assert.That(open.IsCanceled&&back.IsCanceled,Is.True);Assert.That(ui.CachedCount,Is.Zero);Assert.That(live.childCount,Is.Zero);
        }
        [Test]public void InputFieldEscapeOnlyReleasesFocus()
        {
            Mount();Done(ui.Open("popup"));var events=Root("Events").AddComponent<FrameTestEventSystem>();var input=Root("Input").AddComponent<InputField>();events.EnableForTest();EventSystem.current=events;events.SetSelectedGameObject(input.gameObject);
            try {
            Assert.That(EventSystem.current,Is.SameAs(events),"current event system");Assert.That(events.currentSelectedGameObject,Is.SameAs(input.gameObject),"selected input");Assert.That(ui.ReleaseInputFocus(),Is.True,"focus released");Assert.That(events.currentSelectedGameObject,Is.Null);Assert.That(ui.Find("popup").IsOpen,Is.True,"popup preserved"); } finally { events.DisableForTest(); }
        }
        [Test]public void DuplicateManagerIsRejectedWithoutResettingOriginalSingleton()
        {var other=Root("Other").AddComponent<UIManager>();Assert.Throws<InvalidOperationException>(()=>other.Initialize());other.Shutdown();Assert.That(UIManager.Instance,Is.SameAs(ui));}
        private static void Reference(Object target,string field,Object value)
        {var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
        private TMPro.TMP_Text Label(Transform parent,string name)
        {var go=new GameObject(name,typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(parent,false);return go.GetComponent<TMPro.TMP_Text>();}
        private Button Button(Transform parent,string name)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Button));go.transform.SetParent(parent,false);return go.GetComponent<Button>();}
        private void MessagePrefab()
        {
            var message=Prefab<MessageBoxView>("UI/Overlays/MessageBox");
            Reference(message,"title",Label(message.transform,"Title"));Reference(message,"body",Label(message.transform,"Body"));
            Reference(message,"confirm",Button(message.transform,"Confirm"));Reference(message,"cancel",Button(message.transform,"Cancel"));
        }
        [Test]public void MessagesQueueOutsideCommandsAndReuseClosedInstance()
        {
            MessagePrefab();Mount();var first=ui.ShowMessageBox("First","Body");var instance=ui.Find("UI/Overlays/MessageBox");var second=ui.ShowMessageBox("Second","Body");
            Assert.That(first.IsCompleted||second.IsCompleted,Is.False);
            Done(ui.Back());Assert.That(Done(first),Is.EqualTo(MessageBoxResult.Cancelled));Assert.That(second.IsCompleted,Is.False);
            Assert.That(ui.Find("UI/Overlays/MessageBox"),Is.SameAs(instance));
            instance.transform.Find("Confirm").GetComponent<Button>().onClick.Invoke();
            Assert.That(Done(second),Is.EqualTo(MessageBoxResult.Confirmed));Assert.That(instance.IsOpen,Is.False);Assert.That(loads,Is.EqualTo(2));
        }
        [Test]public void MessageUnderNewPanelHidesAndRestoresWithoutCompleting()
        {
            MessagePrefab();Mount();var message=ui.ShowMessageBox("Title","Body");var popup=ui.Find("UI/Overlays/MessageBox");
            Done(ui.Open("next"));Assert.That(popup.IsOpen,Is.True);Assert.That(popup.IsVisible,Is.False);Assert.That(message.IsCompleted,Is.False);
            Done(ui.Back());Assert.That(popup.IsVisible,Is.True);Done(ui.Back());Assert.That(Done(message),Is.EqualTo(MessageBoxResult.Cancelled));
        }
        [Test]public void UnmountCompletesAllMessagesAndClearsSceneCache()
        {
            MessagePrefab();Mount();var first=ui.ShowMessageBox("First","Body");var second=ui.ShowMessageBox("Second","Body");
            ui.Unmount(live);Assert.That(Done(first),Is.EqualTo(MessageBoxResult.SceneClosed));Assert.That(Done(second),Is.EqualTo(MessageBoxResult.SceneClosed));
            Assert.That(ui.CachedCount,Is.Zero);Assert.That(ui.Frames.Count,Is.Zero);
        }
        [Test]public void ToastRequestsReuseFixedOverlayWithoutBlockingNavigation()
        {
            var toast=Prefab<TransientTextView>("UI/Overlays/Toast",Placement.Independent,OrderPolicy.Fixed,500);
            Set(toast,"blocksInput",false);Set(toast,"acceptsRaycasts",false);Reference(toast,"label",Label(toast.transform,"Text"));Mount();
            ui.ShowToast("First");ui.ShowToast("Second");var instance=ui.Find(toast.Key);Done(ui.Open("next"));Assert.That(instance.IsVisible,Is.True);Assert.That(instance.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            Done(ui.Close(toast.Key));Assert.That(instance.IsOpen,Is.True);Assert.That(instance.GetComponentInChildren<TMPro.TMP_Text>().text,Is.EqualTo("Second"));
            Done(ui.Close(toast.Key));Assert.That(instance.IsOpen,Is.False);
        }
        [Test]public void SameBundleDuplicateKeysLeaveNoPartialRegistration()
        {
            Mount();var parent=Prefab<FrameTestPanel>("duplicateBundle");var child=Prefab<FrameTestPopup>("child");Set(child,"key","duplicateBundle");child.transform.SetParent(parent.transform);
            Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("duplicateBundle")));Assert.That(ui.CachedCount,Is.EqualTo(1));Assert.That(staging.childCount,Is.Zero);
        }
        [Test]public void FramePopupCannotBecomeMainRoot()
        {Assert.Throws<InvalidOperationException>(()=>Done(ui.Mount(live,staging,"popup","popup",Load)));Assert.That(ui.CachedCount,Is.Zero);Assert.That(live.childCount,Is.Zero);}
        [Test]public void WrongRequestedKeyRollsBackEntireLoadedBundle()
        {Mount();Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("absent",null,"next")));Assert.That(ui.Find("next"),Is.Null);Assert.That(ui.CachedCount,Is.EqualTo(1));}
        [Test]public void ClosingHiddenPanelCannotDeleteAnUnderlyingFrame()
        {Mount();Done(ui.Open("next"));Assert.That(Done(ui.Close("root")),Is.False);Assert.That(ui.Frames.Count,Is.EqualTo(2));}
        [Test]public void UiLifecycleEntryPointsAreInternal()
        {
            foreach(var name in new[]{"OpenInternal","HideInternal","ShowInternal","CloseInternal","BackInternal"})
                Assert.That(typeof(UIBase).GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).IsAssembly,Is.True);
        }
        [Test]public void RuntimeWindowsDoNotAssembleUiOrToggleGameObjects()
        {
            foreach(var file in System.IO.Directory.GetFiles("Assets/_Project/Scripts/Runtime/UI","*.cs"))
            {
                var name=System.IO.Path.GetFileName(file);var source=System.IO.File.ReadAllText(file);
                Assert.That(source,Does.Not.Contain("new GameObject("),name);Assert.That(source,Does.Not.Contain("AddComponent<"),name);
                if(name!="UIBase.cs" && name!="BootStatusView.cs" && name!="BootDialogSystem.cs")Assert.That(source,Does.Not.Contain(".SetActive("),name);
            }
        }
        [Test]public void RegistrationOccursUnderInactiveRootBeforeAnyEnableCallback()
        {
            Mount();var parent=Prefab<FrameTestPanel>("bundle");parent.gameObject.AddComponent<FrameEnableProbe>();parent.gameObject.SetActive(true);
            var child=Prefab<FrameTestPopup>("child");Set(child,"key","root");child.transform.SetParent(parent.transform);FrameEnableProbe.Enabled=0;
            Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("bundle")));Assert.That(FrameEnableProbe.Enabled,Is.Zero);
            var good=Prefab<FrameTestPopup>("good");good.gameObject.AddComponent<FrameEnableProbe>();good.gameObject.SetActive(true);FrameEnableProbe.Enabled=0;
            Done(ui.Open("good"));Assert.That(FrameEnableProbe.Enabled,Is.EqualTo(1));
        }
        [Test]public void OpenHookFailureRestoresPreviousFrameAndQueueContinues()
        {
            Prefab<FailingFramePanel>("failing");Mount();Done(ui.Open("popup"));Assert.Throws<InvalidOperationException>(()=>Done(ui.Open("failing")));
            Assert.That(ui.Find("root").IsVisible&&ui.Find("popup").IsVisible,Is.True);Assert.That(ui.Find("failing").IsOpen,Is.False);
            Done(ui.Open("next"));Assert.That(ui.Frames.Count,Is.EqualTo(2));
        }
        [Test]public async Task CancelledMountCannotRegisterAfterNewSession()
        {
            var pending=new TaskCompletionSource<GameObject>();var first=ui.Mount(live,staging,"root","root",_=>pending.Task);
            ui.Unmount(live);Assert.That(first.IsCanceled,Is.True);
            var next=ui.Mount(live,staging,"root","root",Load);pending.SetResult(prefabs["root"]);
            await Task.WhenAny(next,Task.Delay(3000));
            Assert.That(next.IsCompleted,Is.True,"draining="+typeof(UIManager).GetField("draining",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(ui));
            Done(next);Assert.That(ui.CachedCount,Is.EqualTo(1));Assert.That(ui.Frames.Count,Is.EqualTo(1));
        }
    }
}
