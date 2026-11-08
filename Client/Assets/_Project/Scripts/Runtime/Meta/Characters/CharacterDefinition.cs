using UnityEngine;

namespace PocketArcade.Meta
{
    [CreateAssetMenu(menuName = "Pocket Arcade/Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite portrait;
        [SerializeField] private Color themeColor = Color.white;
        [SerializeField] private bool defaultUnlocked = true;
        [SerializeField] private string abilityId;
        public string Id => id;
        public string DisplayName => L10n.Text("character." + id + ".name");
        public Sprite SourcePortrait => portrait;
        public Sprite Portrait
        {
            get
            {
                var loaded = ResourceManager.Instance != null ? ResourceManager.Instance.GetLoaded<Sprite>(BootResourceManifest.Portrait(id)) : null;
                return loaded != null ? loaded : portrait;
            }
        }
        public Color ThemeColor => themeColor;
        public bool DefaultUnlocked => defaultUnlocked;
        public string AbilityId => abilityId;
    }
}
