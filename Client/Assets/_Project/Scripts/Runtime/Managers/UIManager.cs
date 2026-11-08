using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace PocketArcade
{
    [DisallowMultipleComponent]
    public sealed class UIManager : PersistentManager
    {
        private sealed class Command
        {
            public Func<Task<object>> Run;
            public readonly TaskCompletionSource<object> Result = new TaskCompletionSource<object>();
            public int Session;
        }
        public static UIManager Instance { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool HasView => activeView != null;
        public UIPage? CurrentPanel => frames.Count == 0 ? (UIPage?)null : Page(frames[frames.Count-1].Panel.Key);
        public IReadOnlyList<PanelFrame> Frames => frames.AsReadOnly();
        public int CachedCount => cache.Count;
        public event Action RootBackRequested;
        private readonly Queue<Command> commands = new Queue<Command>();
        private readonly Dictionary<string, UIBase> cache = new Dictionary<string, UIBase>(StringComparer.Ordinal);
        private readonly List<PanelFrame> frames = new List<PanelFrame>();
        private readonly List<UIPopup> independent = new List<UIPopup>();
        private readonly List<GameObject> bundles = new List<GameObject>();
        private Transform liveRoot, inactiveRoot;
        private Func<string, Task<GameObject>> loader;
        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private UISceneView activeView;
        private UIFeedback feedback;
        private bool draining, resetting;
        private int session, baseOrder;
        private Command running;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
        public void Initialize()
        {
            if (Instance != null && Instance != this) throw Error("ui.error.duplicate");
            if (IsInitialized) return;
            Persist(); Instance = this; IsInitialized = true;
        }
        private static InvalidOperationException Error(string key) => new InvalidOperationException(L10n.Text(key));
        private Task<T> Enqueue<T>(Func<Task<T>> action)
        {
            if (!IsInitialized || resetting) return Task.FromException<T>(Error("ui.error.initialize"));
            var command = new Command { Session = session, Run = async () => (object)await action() };
            commands.Enqueue(command);
            if (!draining) Observe(Drain());
            return Result<T>(command.Result.Task);
        }
        private static async Task<T> Result<T>(Task<object> task) => (T)await task;
        private async Task Drain()
        {
            draining = true;
            try
            {
                while (commands.Count > 0)
                {
                    var next = commands.Dequeue(); running = next;
                    try
                    {
                        if (next.Session != session) throw new OperationCanceledException();
                        var value = await next.Run();
                        if (next.Session != session) throw new OperationCanceledException();
                        next.Result.TrySetResult(value);
                    }
                    catch (OperationCanceledException) { next.Result.TrySetCanceled(); }
                    catch (Exception error) { next.Result.TrySetException(error); }
                    finally { if (running == next) running = null; }
                }
            }
            finally { draining = false; }
        }
        public static async void Observe(Task task)
        { try { await task; } catch (OperationCanceledException) { } catch (Exception error) { Debug.LogException(error); } }
        public Task<bool> Mount(Transform live, Transform inactive, string bundlePath, string rootKey,
            Func<string,Task<GameObject>> prefabLoader = null, int sortingBase = 100) => Enqueue(async () =>
        {
            if (liveRoot != null || live == null || inactive == null || inactive.gameObject.activeInHierarchy || live == inactive)
                throw Error("ui.error.host");
            liveRoot = live; inactiveRoot = inactive; baseOrder = sortingBase;
            loader = prefabLoader ?? LoadFromBoot;
            try
            {
                var root = await Resolve(rootKey, bundlePath);
                if (!(root is UIPanel)) throw Error("ui.error.root");
                OpenResolved(root, null); return true;
            }
            catch { ClearWindows(); liveRoot = inactiveRoot = null; loader = null; throw; }
        });
        private Task<GameObject> LoadFromBoot(string path)
        {
            var resource = ResourceManager.Instance;
            if (resource == null || !resource.IsPreloaded) throw Error("ui.error.resources");
            var prefab = resource.GetLoaded<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException(L10n.Format("ui.error.prefab", path));
            return Task.FromResult(prefab);
        }
        private async Task<UIBase> Resolve(string key, string path)
        {
            if (cache.TryGetValue(key, out var known)) return known;
            if (loader == null || liveRoot == null || inactiveRoot == null) throw Error("ui.error.host");
            int version = session;
            var token = lifetime.Token;
            var cancelled = new TaskCompletionSource<bool>();
            var pending = loader(path ?? key);
            using (token.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(pending, cancelled.Task) != pending)
                {
                    _ = pending.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    throw new OperationCanceledException();
                }
            }
            var prefab = await pending;
            if (version != session || token.IsCancellationRequested) throw new OperationCanceledException();
            if (prefab == null) throw new InvalidOperationException(L10n.Format("ui.error.prefab", path));
            var definitions = prefab.GetComponentsInChildren<UIBase>(true);
            var fixedOrders = new int[definitions.Length];
            for (int i=0;i<definitions.Length;i++) fixedOrders[i] = definitions[i].Canvas != null ? definitions[i].Canvas.sortingOrder : 0;
            var instance = Object.Instantiate(prefab, inactiveRoot, false);
            var windows = instance.GetComponentsInChildren<UIBase>(true);
            var added = new List<UIBase>();
            try
            {
                var keys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var window in windows)
                {
                    window.ValidateDefinition();
                    if (!keys.Add(window.Key) || cache.ContainsKey(window.Key))
                        throw new InvalidOperationException(L10n.Format("ui.error.key", window.Key));
                }
                if (!keys.Contains(key)) throw new InvalidOperationException(L10n.Format("ui.error.key", key));
                // Commit only after all nested definitions have passed validation.
                for (int i=0;i<windows.Length;i++) { var window=windows[i]; window.Bind(this,fixedOrders[i]); cache.Add(window.Key, window); added.Add(window); }
                // UIBase roots use host-relative RectTransforms; prefab presenters keep serialized references.
                foreach (var window in windows) window.transform.SetParent(liveRoot, false);
                if (instance.GetComponent<UIBase>() == null)
                { instance.transform.SetParent(liveRoot, false); instance.SetActive(true); }
                bundles.Add(instance);
                return cache[key];
            }
            catch
            {
                foreach (var window in added) { cache.Remove(window.Key); if (window != null) DestroyWindow(window.gameObject); }
                if (instance != null) DestroyWindow(instance);
                throw;
            }
        }
        public Task<UIBase> Open(string key, object userData = null, string prefabPath = null) => Enqueue(async () =>
        {
            var window = await Resolve(key, prefabPath);
            window.ValidateDefinition();
            OpenResolved(window, userData); return window;
        });
        private void OpenResolved(UIBase window, object data)
        {
            if (window.IsOpen) throw new InvalidOperationException(L10n.Format("ui.error.open", window.Key));
            var previous = frames.Count > 0 ? frames[frames.Count-1] : null;
            if (window is UIPanel panel)
            {
                if (previous != null) Visibility(previous, false);
                var frame = new PanelFrame(panel); frames.Add(frame);
                try { panel.OpenInternal(data); }
                catch { frames.Remove(frame); if (previous != null) Visibility(previous,true); Reorder(); throw; }
            }
            else if (window is UIPopup popup)
            {
                if (window.Placement == Placement.Frame && previous == null) throw Error("ui.error.root");
                var list = window.Placement == Placement.Independent ? independent : previous.MutablePopups;
                list.Add(popup);
                try { popup.OpenInternal(data); }
                catch { list.Remove(popup); Reorder(); throw; }
            }
            else throw Error("ui.error.definition");
            Reorder(); activeView?.DisplayNotice("");
        }
        public Task<bool> Close(string key) => Enqueue(() => Task.FromResult(CloseNow(key)));
        private bool CloseNow(string key)
        {
            if (!cache.TryGetValue(key,out var window) || !window.IsOpen) return false;
            if (window is UIPopup popup && independent.Remove(popup))
            { SafeClose(popup); Reorder(); return true; }
            if (frames.Count == 0) return false;
            var top = frames[frames.Count-1];
            if (window == top.Panel)
            {
                if (frames.Count == 1) return false;
                ClosePopups(top); frames.RemoveAt(frames.Count-1); SafeClose(window);
                Visibility(frames[frames.Count-1], true); Reorder(); return true;
            }
            if (window is UIPopup child && top.MutablePopups.Remove(child))
            { SafeClose(child); Reorder(); return true; }
            return false;
        }
        public Task<bool> Back() => Enqueue(() => Task.FromResult(BackNow()));
        private bool BackNow()
        {
            UIBase target = null;
            foreach (var overlay in independent)
                if (target == null || overlay.Canvas.sortingOrder >= target.Canvas.sortingOrder) target = overlay;
            if (target == null && frames.Count > 0)
            {
                var frame = frames[frames.Count-1];
                target = frame.MutablePopups.Count > 0 ? (UIBase)frame.MutablePopups[frame.MutablePopups.Count-1] : frame.Panel;
            }
            if (target == null) return false;
            if (target.BackInternal()) return true;
            if (frames.Count == 1 && target == frames[0].Panel)
            { RootBackRequested?.Invoke(); return true; }
            if (target.CanCloseOnBack) CloseNow(target.Key);
            return true; // A nonclosable overlay still consumes Back.
        }
        public Task<bool> Home() => Enqueue(() =>
        {
            foreach (var overlay in independent.ToArray()) CloseNow(overlay.Key);
            while (frames.Count > 1) CloseNow(frames[frames.Count-1].Panel.Key);
            if (frames.Count == 1) { ClosePopups(frames[0]); Visibility(frames[0],true); }
            Reorder(); return Task.FromResult(frames.Count > 0);
        });
        private void ClosePopups(PanelFrame frame)
        { for (int i=frame.MutablePopups.Count-1;i>=0;i--) SafeClose(frame.MutablePopups[i]); frame.MutablePopups.Clear(); }
        private static void SafeClose(UIBase window)
        { try { if (window != null) window.CloseInternal(); } catch (Exception error) { Debug.LogException(error); } }
        private static void Visibility(PanelFrame frame, bool visible)
        {
            SetVisibility(frame.Panel, visible);
            foreach (var popup in frame.MutablePopups) SetVisibility(popup, visible);
        }
        private static void SetVisibility(UIBase window, bool visible)
        {
            try { if (visible) window.ShowInternal(); else window.HideInternal(); }
            catch (Exception error) { Debug.LogException(error); }
        }
        private void Reorder()
        {
            UIBase blocker = null;
            foreach (var item in independent)
                if (item.BlocksInput && (blocker == null || item.Canvas.sortingOrder >= blocker.Canvas.sortingOrder)) blocker = item;
            if (frames.Count > 0)
            {
                var top=frames[frames.Count-1]; int order=baseOrder;
                top.Panel.OrderInternal(order);
                foreach (var popup in top.MutablePopups) { order += 10; popup.OrderInternal(order); }
                UIBase modal = blocker;
                for(int i=top.MutablePopups.Count-1; modal == null && i>=0;i--)
                    if(top.MutablePopups[i].BlocksInput) modal=top.MutablePopups[i];
                top.Panel.InputInternal(modal == null);
                foreach(var popup in top.MutablePopups) popup.InputInternal(modal == null || modal == popup);
            }
            foreach(var overlay in independent) overlay.InputInternal(blocker == null || blocker == overlay);
            if (activeView != null && activeView.TryGetComponent<CanvasGroup>(out var chrome))
                chrome.interactable = blocker == null && (frames.Count == 0 || frames[frames.Count-1].MutablePopups.Count == 0);
        }
        public UIBase Find(string key) => cache.TryGetValue(key,out var window) ? window : null;
        public static string Key(UIPage page) => "UI/Panels/" + page;
        private static UIPage? Page(string key)
        { foreach(UIPage page in Enum.GetValues(typeof(UIPage))) if(Key(page)==key) return page; return null; }
        public Task<UIBase> ShowPanel(UIPage page) => Open(Key(page));
        public Task<bool> ShowHome() => Home();
        public void Register(UISceneView view)
        { if(activeView != null && activeView != view) throw Error("ui.error.host"); activeView=view; Reorder(); }
        public void Unregister(UISceneView view) { if(activeView==view) activeView=null; }
        public bool ShowNotice(string text) { if(activeView==null)return false; activeView.DisplayNotice(text); return true; }
        private UIFeedback Feedback() => feedback ?? (feedback = new UIFeedback(this));
        public Task<MessageBoxResult> ShowMessageBox(string title,string body) => Feedback().ShowMessage(title,body);
        public void ShowToast(string text,float seconds=2) => Feedback().Toast(text,seconds);
        public void ShowTooltip(string text,RectTransform target) => Feedback().Tooltip(text,target);
        public void HideTooltip() => Observe(Close("UI/Overlays/Tooltip"));
        public Task<UIBase> OpenPopup(string key) => Open(key);
        public bool ReleaseInputFocus()
        {
            var events=EventSystem.current; var selected=events != null ? events.currentSelectedGameObject : null;
            if(selected==null)return false;
            var tmp=selected.GetComponentInParent<TMP_InputField>();
            var legacy=selected.GetComponentInParent<InputField>();
            if(tmp==null && legacy==null)return false;
            if(tmp!=null)tmp.DeactivateInputField(); if(legacy!=null)legacy.DeactivateInputField();
            events.SetSelectedGameObject(null);return true;
        }
        private void Update()
        {
            if(IsInitialized && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { if(!ReleaseInputFocus())Observe(Back()); }
        }
        public void Unmount(Transform owner)
        { if(liveRoot==owner) CancelSession(); }
        private void CancelSession()
        {
            resetting=true;
            session++; lifetime.Cancel(); lifetime.Dispose(); lifetime=new CancellationTokenSource();
            running?.Result.TrySetCanceled();
            while(commands.Count>0)commands.Dequeue().Result.TrySetCanceled();
            feedback?.Dispose(); feedback=null; ClearWindows();
            activeView=null; liveRoot=inactiveRoot=null; loader=null; RootBackRequested=null; resetting=false;
        }
        private void ClearWindows()
        {
            foreach(var window in cache.Values) SafeClose(window);
            // Dispose presenter subscriptions before destroying detached panel controls.
            foreach(var bundle in bundles) if(bundle!=null)DestroyWindow(bundle);
            foreach(var window in cache.Values) if(window!=null)DestroyWindow(window.gameObject);
            cache.Clear();bundles.Clear();frames.Clear();independent.Clear();
        }
        private static void DestroyWindow(GameObject go)
        { go.SetActive(false); if(Application.isPlaying)Object.Destroy(go);else Object.DestroyImmediate(go); }
        public void Shutdown() { IsInitialized=false; CancelSession(); if(Instance==this)Instance=null; }
        private void OnDestroy()=>Shutdown();
    }
}
