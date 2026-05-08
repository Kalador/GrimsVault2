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
        private static readonly object GlobalWriteLock = new();

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

            lock (GlobalWriteLock) // <— NEW: global write lock
                lock (fileLock)        // <— existing per-file lock
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

        // =====================================================================
        // JSON CACHE WRAPPER (LOAD ONCE → MUTATE → SAVE)
        // =====================================================================

        /// <summary>
        /// Strongly‑typed JSON cache wrapper.
        /// Loads once, mutates in memory, saves once.
        /// </summary>
        public sealed class JsonCache<T>
        {
            private readonly VaultCache vaultCache;
            private readonly string filename;
            private readonly Func<T, string> keySelector;
            private readonly Dictionary<string, T> data;

            /// <summary>
            /// Creates a typed JSON cache wrapper.
            /// keySelector must return a stable unique key for each T.
            /// </summary>
            public JsonCache(VaultCache vaultCache, string filename, Func<T, string> keySelector)
            {
                this.vaultCache = vaultCache;
                this.filename = filename;
                this.keySelector = keySelector;

                data = vaultCache.ReadCache<Dictionary<string, T>>(filename)
                       ?? [];
            }

            public bool TryGet(string key, out T? value)
                => data.TryGetValue(key, out value);

            public void Set(string key, T value)
                => data[key] = value;

            public void Set(T value)
                => data[keySelector(value)] = value;

            public void Prune(IEnumerable<string> validKeys)
            {
                var valid = new HashSet<string>(validKeys);

                foreach (var key in data.Keys.ToList())
                {
                    if (!valid.Contains(key))
                        data.Remove(key);
                }
            }

            public IEnumerable<T> Values => data.Values;

            public void Save()
                => vaultCache.WriteCache(filename, data);
        }
    }

    public enum CacheWriteMode
    {
        Overwrite = 0,
        Merge = 1
    }
}
