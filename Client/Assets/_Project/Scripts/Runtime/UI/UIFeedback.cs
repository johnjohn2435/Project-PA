using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace PocketArcade.UI
{
    public sealed class MessageData
    {
        public string Title, Body;
        public MessageBoxResult Answer = MessageBoxResult.Cancelled;
        public readonly TaskCompletionSource<MessageBoxResult> Completion = new TaskCompletionSource<MessageBoxResult>();
    }
    public sealed class TransientData
    {
        public string Text;
        public RectTransform Target;
        public float Seconds;
        public readonly TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
    }
    // Presentation requests wait outside the command queue, so user input never blocks Back/Close.
    internal sealed class UIFeedback : IDisposable
    {
        private readonly UIManager ui;
        private Task messages = Task.CompletedTask, toasts = Task.CompletedTask;
        private readonly List<MessageData> pending = new List<MessageData>();
        private readonly List<TransientData> transient = new List<TransientData>();
        private bool disposed;
        public UIFeedback(UIManager ui) { this.ui=ui; }
        public Task<MessageBoxResult> ShowMessage(string title,string body)
        {
            var data=new MessageData {Title=title,Body=body}; pending.Add(data);
            var previous=messages; messages=RunMessage(previous,data); UIManager.Observe(messages);
            return data.Completion.Task;
        }
        private async Task RunMessage(Task previous,MessageData data)
        {
            try
            {
                try { await previous; } catch { }
                if(disposed) { data.Completion.TrySetResult(MessageBoxResult.SceneClosed); return; }
                await ui.Open("UI/Overlays/MessageBox",data);
                await data.Completion.Task;
            }
            catch(OperationCanceledException) { data.Completion.TrySetResult(MessageBoxResult.SceneClosed); }
            catch(Exception error) { data.Completion.TrySetException(error); }
            finally { pending.Remove(data); }
        }
        public void Toast(string text,float seconds)
        {
            var data=new TransientData {Text=text,Seconds=Mathf.Max(.1f,seconds)}; transient.Add(data);
            var previous=toasts; toasts=RunToast(previous,data); UIManager.Observe(toasts);
        }
        private async Task RunToast(Task previous,TransientData data)
        {
            try
            {
                try { await previous; } catch { }
                if(disposed)return;
                await ui.Open("UI/Overlays/Toast",data); await data.Completion.Task;
            }
            finally { transient.Remove(data); }
        }
        public void Tooltip(string text,RectTransform target)
        {
            UIManager.Observe(ui.Close("UI/Overlays/Tooltip"));
            if(target!=null)UIManager.Observe(ui.Open("UI/Overlays/Tooltip",new TransientData {Text=text,Target=target}));
        }
        public void Dispose()
        {
            disposed=true;
            foreach(var item in pending.ToArray())item.Completion.TrySetResult(MessageBoxResult.SceneClosed);
            foreach(var item in transient.ToArray())item.Completion.TrySetResult(false);
            pending.Clear();transient.Clear();
        }
    }
}
