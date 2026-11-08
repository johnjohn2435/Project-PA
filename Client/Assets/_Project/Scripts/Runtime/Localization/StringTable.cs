using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
namespace PocketArcade
{
    public enum GameLanguage { Korean, English }
    [Serializable] public sealed class StringRow { public string key, ko, en; }
    [Serializable] internal sealed class StringTableFile { public int version; public StringRow[] rows; }
    public sealed class StringTable
    {
        public const string ResourcePath = "Data/StringTable";
        private readonly Dictionary<string, StringRow> rows = new Dictionary<string, StringRow>(StringComparer.Ordinal);
        public IEnumerable<StringRow> Rows => rows.Values;
        public bool Contains(string key) => key != null && rows.ContainsKey(key);
        public static StringTable Parse(string json)
        {
            var file = JsonUtility.FromJson<StringTableFile>(json);
            if (file == null || file.version != 1 || file.rows == null || file.rows.Length == 0)
                throw new FormatException("String table requires version 1 and rows.");
            var table = new StringTable();
            foreach (var row in file.rows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.key) || row.key != row.key.Trim() ||
                    string.IsNullOrWhiteSpace(row.ko) || string.IsNullOrWhiteSpace(row.en) || table.rows.ContainsKey(row.key))
                    throw new FormatException("Invalid, incomplete or duplicate string row: " + row?.key);
                var ko = Arguments(row.ko); var en = Arguments(row.en);
                if (!ko.SetEquals(en)) throw new FormatException("Translation placeholders differ: " + row.key);
                table.rows.Add(row.key, row);
            }
            return table;
        }
        private static HashSet<int> Arguments(string text)
        {
            var result = new HashSet<int>();
            foreach (Match match in Regex.Matches(text, @"(?<!\{)\{(\d+)(?:[^{}]*)\}(?!\})"))
            {
                int index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (index > 15) throw new FormatException("At most 16 string arguments are supported.");
                result.Add(index);
            }
            // Validate escaped braces and composite formatting as well as matching argument sets.
            string.Format(CultureInfo.InvariantCulture, text, new object[16]);
            return result;
        }
        public string Get(string key, GameLanguage language)
        {
            if (!Contains(key)) return "[" + key + "]";
            return language == GameLanguage.Korean ? rows[key].ko : rows[key].en;
        }
        public string Format(string key, GameLanguage language, params object[] args) =>
            string.Format(language == GameLanguage.Korean ? CultureInfo.GetCultureInfo("ko-KR") : CultureInfo.GetCultureInfo("en-US"), Get(key, language), args);
    }
    // Boot owns loading; this lookup facade never loads assets on demand.
    public static class L10n
    {
        public static StringTable Table { get; private set; }
        public static GameLanguage Language { get; private set; } = GameLanguage.Korean;
        public static bool IsReady => Table != null;
        public static event Action Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Table = null; Language = GameLanguage.Korean; Changed = null; }
        public static void Initialize(string json)
        { var parsed = StringTable.Parse(json); Table = parsed; Changed?.Invoke(); }
        public static void SetLanguage(GameLanguage language)
        {
            if (!Enum.IsDefined(typeof(GameLanguage), language)) throw new ArgumentOutOfRangeException(nameof(language));
            if (Language == language) return;
            Language = language; Changed?.Invoke();
        }
        public static string Text(string key) => Table == null ? "[" + key + "]" : Table.Get(key, Language);
        public static string Format(string key, params object[] args) => Table == null ? "[" + key + "]" : Table.Format(key, Language, args);
    }
}
