using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineTool
{
    /// <summary>
    /// General-purpose reference registry MonoBehaviour.
    /// Configure entries in the Inspector (Key + Type + Value),
    /// then retrieve them at runtime by key and type.
    /// </summary>
    [AddComponentMenu("TimelineTool/Reference Hub")]
    public class ReferenceHub : MonoBehaviour
    {
        [SerializeReference]
        private List<ReferenceEntryBase> entries = new();

        private Dictionary<string, ReferenceEntryBase> _lookup;

        // -----------------------------------------------------------------------

        private void Awake()      => BuildLookup();
        private void OnValidate() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ReferenceEntryBase>(StringComparer.Ordinal);
            foreach (var entry in entries)
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    _lookup[entry.Key] = entry;
        }

        private void EnsureLookup()
        {
            if (_lookup == null) BuildLookup();
        }

        // -----------------------------------------------------------------------
        // Public API

        /// <summary>
        /// Returns the value stored under <paramref name="key"/> as type T.
        /// Logs a warning and returns default(T) when the key is missing.
        /// </summary>
        public T Get<T>(string key) where T : ReferenceEntry<T>
        {
            EnsureLookup();
            if (_lookup.TryGetValue(key, out var entryBase) && entryBase is T entry) { return entry; }

            Debug.LogWarning($"[ReferenceHub] Key \"{key}\" not found on \"{name}\".", this);
            return default;
        }

        /// <summary>
        /// Tries to get the value stored under <paramref name="key"/> as type T.
        /// Returns false silently (no warning) when the key is missing.
        /// </summary>
        public bool TryGet<T>(string key, out T value) where T : ReferenceEntry<T>
        {
            EnsureLookup();
            if (_lookup.TryGetValue(key, out var entryBase) && entryBase is T entry)
            {
                value = entry;
                return value != null;
            }
            value = default;
            return false;
        }

        /// <summary>Returns true if the given key exists in the registry.</summary>
        public bool Contains(string key)
        {
            EnsureLookup();
            return _lookup.ContainsKey(key);
        }
    }
}
