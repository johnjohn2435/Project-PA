using UnityEngine;
namespace PocketArcade.Characters
{
    public sealed class CharacterRigController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private static readonly int Speed=Animator.StringToHash("Speed");
        public static CharacterRigController SpawnForWorld(ResourceManager resources, string id, Transform parent, SpriteRenderer marker, float scale)
        {
            var instance = resources.Spawn(BootResourceManifest.CharacterPrefab(id), parent);
            instance.transform.localScale *= scale;
            foreach (var part in instance.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = marker.gameObject.layer;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sortingOrder += marker.sortingOrder - 5;
            marker.enabled = false;
            return instance.GetComponent<CharacterRigController>();
        }
        public void SetPaused(bool paused) => animator.speed = paused ? 0f : 1f;
        public void SetMovement(Vector2 direction)
        {
            animator.SetFloat(Speed,direction.magnitude);
            if(Mathf.Abs(direction.x)>.001f)
            {var scale=transform.localScale;scale.x=Mathf.Abs(scale.x)*(direction.x<0?-1:1);transform.localScale=scale;}
        }
    }
}
