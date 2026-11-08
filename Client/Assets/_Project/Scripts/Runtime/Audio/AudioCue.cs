namespace PocketArcade
{
    public enum AudioCue { Confirm, Cancel, Place, Clear, Combo, Gravity, GameOver }
    public static class AudioPaths
    {
        public const string GameBgm="Audio/BGM/Game";
        public static string Cue(AudioCue value)=>"Audio/SFX/"+value;
    }
}
