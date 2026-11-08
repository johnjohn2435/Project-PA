using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UI
{
    public sealed class CharacterRigPreview : MonoBehaviour
    {
        public const string CameraPath="Characters/Shared/PreviewCamera";
        [SerializeField] private RawImage target;
        private ResourceManager resources;
        private GameObject world,character;
        private string current;
        public bool IsAvailable=>world!=null&&character!=null;
        private void OnEnable()
        {
            var data=DataManager.Instance;bool shown=data!=null&&data.IsInitialized&&ShowCharacter(data.SelectedCharacterId);
            var fallback=transform.parent.GetComponent<Image>();if(fallback!=null)fallback.enabled=!shown;
        }
        public bool ShowCharacter(string id)
        {
            resources=ResourceManager.Instance;
            if(resources==null||!resources.IsPreloaded||resources.GetLoaded<GameObject>(BootResourceManifest.CharacterPrefab(id))==null)
            {Release();return false;}
            if(current==id&&IsAvailable)return true;
            if(world==null)world=resources.Spawn(CameraPath,null);
            if(character!=null)resources.Release(character);
            character=resources.Spawn(BootResourceManifest.CharacterPrefab(id),world.transform.Find("Anchor"));current=id;target.enabled=true;return true;
        }
        private void Release()
        {
            if(resources!=null){if(character!=null)resources.Release(character);if(world!=null)resources.Release(world);}
            character=world=null;current=null;if(target!=null)target.enabled=false;
        }
        private void OnDisable()=>Release();
    }
}
