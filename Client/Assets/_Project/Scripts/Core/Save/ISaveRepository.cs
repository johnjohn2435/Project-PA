namespace PocketArcade.Core.Save
{
    public interface ISaveRepository
    {
        SaveData Load();
        void Save(SaveData data);
    }
}
