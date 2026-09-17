namespace PocketArcade.Core
{
    public static class ProjectDefaults
    {
        public const int ReferenceWidth = 1080;
        public const int ReferenceHeight = 1920;
        public const int TargetFrameRate = 60;
        public const string ProductName = "PocketArcade";
        public const string AndroidApplicationId = "com.johnjohn2435.pocketarcade";
    }

    public static class SceneIds
    {
        public const string Boot = "Boot";
        public const string MainHub = "MainHub";
        public const string BlockGravity = "BlockGravity";
        public const string Directory = "Assets/_Project/Scenes/";
        public static string PathFor(string sceneName) => Directory + sceneName + ".unity";
    }
}
