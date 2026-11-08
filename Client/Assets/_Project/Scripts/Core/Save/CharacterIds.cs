namespace PocketArcade.Core.Save
{
    public static class CharacterIds
    {
        public const string Cat = "char_cat", Dog = "char_dog", Hamster = "char_hamster", Poodle = "char_poodle";
        // Compatibility exists only at the save-data migration boundary.
        public static string FromLegacy(string id)
        {
            switch(id) { case "char_momo": return Cat; case "char_bori": return Dog;
                case "char_nabi": return Hamster; default: return id; }
        }
    }
}
