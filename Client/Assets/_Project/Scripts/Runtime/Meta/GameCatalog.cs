using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketArcade.Meta
{
    [CreateAssetMenu(menuName = "Pocket Arcade/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        [SerializeField] private CharacterDefinition[] characters;
        [SerializeField] private MiniGameDefinition[] miniGames;
        public IReadOnlyList<CharacterDefinition> Characters => characters;
        public IReadOnlyList<MiniGameDefinition> MiniGames => miniGames;

        public CharacterDefinition Character(string id)
        {
            foreach (var character in characters) if (character.Id == id) return character;
            throw new ArgumentException("Unknown character: " + id);
        }

        public MiniGameDefinition MiniGame(string id)
        {
            foreach (var game in miniGames) if (game.Id == id) return game;
            return null;
        }

        public void Validate()
        {
            if (characters == null || characters.Length == 0 || miniGames == null || miniGames.Length == 0)
                throw new InvalidOperationException("Game Catalog requires characters and mini games.");
            var ids = new HashSet<string>();
            foreach (var character in characters)
                if (character == null || string.IsNullOrWhiteSpace(character.Id) || !ids.Add(character.Id))
                    throw new InvalidOperationException("Character IDs must be present and unique.");
            ids.Clear();
            foreach (var game in miniGames)
                if (game == null || string.IsNullOrWhiteSpace(game.Id) || !ids.Add(game.Id) ||
                    (game.IsAvailable && string.IsNullOrWhiteSpace(game.SceneName)))
                    throw new InvalidOperationException("Mini game IDs/scenes are missing or duplicated.");
        }
    }
}
