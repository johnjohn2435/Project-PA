using System;
using System.IO;
using System.Text;
using PocketArcade.Core.Save;
using UnityEngine;

namespace PocketArcade.Save
{
    public sealed class JsonSaveRepository : ISaveRepository
    {
        private readonly string path;
        private readonly Action<string> report;
        public JsonSaveRepository(string path, Action<string> report = null)
        {
            this.path = Path.GetFullPath(path);
            this.report = report;
        }

        public SaveData Load()
        {
            if (TryRead(path, out var data)) return data;
            if (TryRead(path + ".bak", out data))
            {
                report?.Invoke("Recovered the local save from its backup.");
                return data;
            }
            return new SaveData();
        }

        private bool TryRead(string file, out SaveData data)
        {
            data = null;
            if (!File.Exists(file)) return false;
            try
            {
                string json = File.ReadAllText(file, Encoding.UTF8).Trim();
                if (!json.StartsWith("{", StringComparison.Ordinal) || !json.EndsWith("}", StringComparison.Ordinal))
                    throw new ArgumentException("Save is not a JSON object.");
                data = new SaveData();
                // 기본값 위에 덮어써 누락된 bool 필드도 true 기본값을 유지한다.
                JsonUtility.FromJsonOverwrite(json, data);
                data.Normalize();
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                report?.Invoke("Could not read local save: " + error.Message);
                data = null;
                return false;
            }
        }

        public void Save(SaveData data)
        {
            var normalized = data.Copy();
            normalized.Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(normalized, true));
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path))
                    File.Replace(temp, path, path + ".bak");
                else
                    File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
