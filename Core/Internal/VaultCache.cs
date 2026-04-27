using System.Text.Json;
using System.Collections.Concurrent;

namespace Vault.Core
{
    /// <summary>
    /// File‑backed JSON cache. Stores and loads any serializable type.
    /// Writes go to App_Data. Thread‑safe per file.
    /// </summary>
    public class VaultCache
    {
        private const string RootPath = "App_Data";
        private readonly ConcurrentDictionary<string, object> _fileLocks = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public VaultCache()
        {
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
        }

        /// <summary>
        /// Writes data to a cache file.
        /// Overwrite = replace file.
        /// Merge = load existing, merge, then write.
        /// </summary>
        public void WriteCache<T>(string filename, T data, CacheWriteMode mode = CacheWriteMode.Overwrite)
        {
            if (mode == CacheWriteMode.Overwrite)
            {
                WriteOverwrite(filename, data);
                return;
            }

            var existing = ReadCache<T>(filename);

            if (existing == null)
            {
                WriteOverwrite(filename, data);
                return;
            }

            var merged = Merge(existing, data);
            WriteOverwrite(filename, merged);
        }

        /// <summary>
        /// Loads and deserializes a cache file.
        /// Returns default if missing or invalid.
        /// </summary>
        public T? ReadCache<T>(string filename)
        {
            var path = Path.Combine(RootPath, filename);

            if (!File.Exists(path))
                return default;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        // ------------------------------------------------------------
        // Internal helpers
        // ------------------------------------------------------------

        private void WriteOverwrite<T>(string filename, T data)
        {
            var path = Path.Combine(RootPath, filename);
            var fileLock = _fileLocks.GetOrAdd(path, _ => new object());

            lock (fileLock)
            {
                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(path, json);
            }
        }

        /// <summary>
        /// Basic merge support:
        /// - Lists: concat + distinct
        /// - Dictionaries: overwrite keys
        /// - Everything else: incoming replaces existing
        /// </summary>
        private static T Merge<T>(T existing, T incoming)
        {
            if (existing is IList<object> listA && incoming is IList<object> listB)
            {
                var combined = listA.Concat(listB).Distinct().ToList();
                return (T)(object)combined;
            }

            if (existing is IDictionary<string, object> dictA &&
                incoming is IDictionary<string, object> dictB)
            {
                foreach (var kvp in dictB)
                    dictA[kvp.Key] = kvp.Value;

                return (T)(object)dictA;
            }

            return incoming;
        }
    }

    /// <summary>
    /// Overwrite = replace file (default).
    /// Merge = load existing and merge before writing.
    /// </summary>
    public enum CacheWriteMode
    {
        Overwrite = 0,
        Merge = 1
    }
}
