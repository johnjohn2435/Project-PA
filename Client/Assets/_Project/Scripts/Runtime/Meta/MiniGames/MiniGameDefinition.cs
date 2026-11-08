using UnityEngine;

namespace PocketArcade.Meta
{
    [CreateAssetMenu(menuName = "Pocket Arcade/Mini Game")]
    public sealed class MiniGameDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string scoreKey;
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private string sceneName;
        [SerializeField] private bool isAvailable;
        public string Id => id;
        public string ScoreKey => string.IsNullOrWhiteSpace(scoreKey) ? id : scoreKey;
        public string DisplayName => L10n.Text("game." + id + ".name");
        public Sprite Thumbnail => thumbnail;
        public string SceneName => sceneName;
        public bool IsAvailable => isAvailable;
    }
}
