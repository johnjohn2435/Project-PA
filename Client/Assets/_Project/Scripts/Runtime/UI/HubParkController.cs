using System;
using PocketArcade.Meta;
using UnityEngine;

namespace PocketArcade.UI
{
    public sealed class HubParkController : IDisposable
    {
        public const string WorldPath = "Hub/ParkWorld";
        private static Vector2? returnPosition;
        private readonly ResourceManager resources;
        private readonly GameCatalog catalog;
        private readonly Action<string> enter;
        private readonly Camera previousCamera;
        private readonly bool previousCameraEnabled;
        private bool inputAllowed;
        private Vector2 lastPosition;
        public HubParkWorld World { get; }
        public string NearbyGameId { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetVisit() => returnPosition = null;
        public HubParkController(ResourceManager source, GameCatalog games, Action<string> enterGame)
        {
            resources = source; catalog = games; enter = enterGame;
            previousCamera = Camera.main;
            previousCameraEnabled = previousCamera != null && previousCamera.enabled;
            World = resources.Spawn(WorldPath, null).GetComponent<HubParkWorld>();
            World.Initialize(resources, catalog, returnPosition);
            lastPosition = World.Position;
            if (previousCamera != null) previousCamera.enabled = false;
        }
        public void Step(Vector2 input, bool allowed, float delta)
        {
            if (World == null || !World.IsReady) { inputAllowed = false; return; }
            inputAllowed = allowed && !(SceneFlowManager.Instance?.IsLoading ?? false);
            var before = World.Position;
            Vector2 movement = inputAllowed ? Vector2.ClampMagnitude(input, 1) : Vector2.zero;
            var position = HubParkNavigation.Move(before, movement * (3.1f * Mathf.Clamp(delta, 0, .1f)), World.WalkArea, World.Obstacles);
            World.Present(position, position == before ? Vector2.zero : movement);
            lastPosition = position;
            NearbyGameId = FindEntrance(position);
        }
        private string FindEntrance(Vector2 position)
        {
            string nearby = null; float best = float.MaxValue;
            foreach (var entrance in World.Entrances)
            {
                if (catalog.MiniGame(entrance.gameId)?.IsAvailable != true) continue;
                float distance = (position - entrance.position).sqrMagnitude;
                if (distance <= entrance.radius * entrance.radius && distance < best)
                { nearby = entrance.gameId; best = distance; }
            }
            return nearby;
        }
        public void Enter()
        {
            if (!inputAllowed || World == null || !World.IsReady || (SceneFlowManager.Instance?.IsLoading ?? false)) return;
            var id = FindEntrance(World.Position);
            if (id == null) return;
            inputAllowed = false;
            returnPosition = World.Position;
            enter(id);
        }
        public void Refresh(string characterId)
        { World.ShowCharacter(characterId); World.RefreshLabels(catalog); }
        public void Dispose()
        {
            returnPosition = lastPosition;
            if (World != null) resources.Release(World.gameObject);
            if (previousCamera != null) previousCamera.enabled = previousCameraEnabled;
        }
    }
}
