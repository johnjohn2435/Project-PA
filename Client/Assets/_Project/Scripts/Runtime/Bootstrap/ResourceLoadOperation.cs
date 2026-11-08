using System;
using UnityEngine;
namespace PocketArcade
{
    public interface IResourceLoadOperation
    {
        bool IsDone { get; }
        float Progress { get; }
        UnityEngine.Object Asset { get; }
    }
    internal sealed class ResourceLoadOperation : IResourceLoadOperation
    {
        private readonly ResourceRequest request;
        public ResourceLoadOperation(string path, Type type) => request = Resources.LoadAsync(path, type);
        public bool IsDone => request.isDone;
        public float Progress => request.progress;
        public UnityEngine.Object Asset => request.asset;
    }
}
