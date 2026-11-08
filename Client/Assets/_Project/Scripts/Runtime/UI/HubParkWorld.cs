using System;
using PocketArcade.Characters;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;

namespace PocketArcade.UI
{
    public sealed class HubParkWorld : MonoBehaviour, ILocalizedDynamicText
    {
        [Serializable] public sealed class Entrance
        {
            public string gameId;
            public Vector2 position;
            public float radius = 1.05f;
            public TMP_Text label;
        }
        [SerializeField] private Camera parkCamera;
        [SerializeField] private Transform actorAnchor;
        [SerializeField] private Entrance[] entrances;
        [SerializeField] private Rect walkArea;
        [SerializeField] private Rect[] obstacles;
        [SerializeField] private Vector2 spawnPoint;
        private ResourceManager resources;
        private CharacterRigController actor;
        private string selectedId;
        public Camera ParkCamera => parkCamera;
        public bool IsReady => actorAnchor != null && parkCamera != null;
        public Vector2 Position => actorAnchor.position;
        public Rect WalkArea => walkArea;
        public Rect[] Obstacles => obstacles;
        public Entrance[] Entrances => entrances;
        public string CharacterId => selectedId;
        public System.Collections.Generic.IEnumerable<TMP_Text> DynamicTextLabels
        { get { foreach (var entrance in entrances) yield return entrance.label; } }
        public void Initialize(ResourceManager source, GameCatalog catalog, Vector2? returnPoint)
        {
            resources = source;
            actorAnchor.position = returnPoint ?? spawnPoint;
            RefreshLabels(catalog);
        }
        public void RefreshLabels(GameCatalog catalog)
        {
            foreach (var entrance in entrances)
                entrance.label.text = catalog.MiniGame(entrance.gameId)?.DisplayName ?? string.Empty;
        }
        public void ShowCharacter(string id)
        {
            if (selectedId == id && actor != null) return;
            if (actor != null) resources.Release(actor.gameObject);
            var instance = resources.Spawn(BootResourceManifest.CharacterPrefab(id), actorAnchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale *= .43f;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true)) r.sortingOrder += 60;
            actor = instance.GetComponent<CharacterRigController>();
            selectedId = id;
        }
        public void Present(Vector2 position, Vector2 movement)
        {
            actorAnchor.position = new Vector3(position.x, position.y, 0);
            actor?.SetMovement(movement);
            parkCamera.orthographicSize = Mathf.Max(6.9f, 5.25f / Mathf.Max(.1f, parkCamera.aspect));
        }
        private void OnDestroy()
        {
            if (resources != null && actor != null) resources.Release(actor.gameObject);
        }
    }
}
