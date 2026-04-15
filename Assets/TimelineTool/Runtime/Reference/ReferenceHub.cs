using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineTool
{
    /// <summary>
    /// General-purpose reference registry MonoBehaviour.
    /// Configure entries in the Inspector (Key + Value),
    /// then retrieve them at runtime by key and type.
    /// Each ReferenceType has its own serialized list.
    /// </summary>
    [AddComponentMenu("TimelineTool/Reference Hub")]
    public class ReferenceHub : MonoBehaviour
    {
        [SerializeField] private List<GameObjectEntry>    gameObjectEntries    = new();
        [SerializeField] private List<TransformEntry>     transformEntries     = new();
        [SerializeField] private List<MonoBehaviourEntry> monoBehaviourEntries = new();
        [SerializeField] private List<IntEntry>           intEntries           = new();
        [SerializeField] private List<FloatEntry>         floatEntries         = new();
        [SerializeField] private List<BoolEntry>          boolEntries          = new();
        [SerializeField] private List<StringEntry>        stringEntries        = new();
        [SerializeField] private List<Vector2Entry>       vector2Entries       = new();
        [SerializeField] private List<Vector3Entry>       vector3Entries       = new();
        [SerializeField] private List<ColorEntry>         colorEntries         = new();

        private Dictionary<string, ReferenceEntryBase> _lookup;

        // -----------------------------------------------------------------------

        private void Awake()      => BuildLookup();
        private void OnValidate() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ReferenceEntryBase>(StringComparer.Ordinal);
            AddToLookup(gameObjectEntries);
            AddToLookup(transformEntries);
            AddToLookup(monoBehaviourEntries);
            AddToLookup(intEntries);
            AddToLookup(floatEntries);
            AddToLookup(boolEntries);
            AddToLookup(stringEntries);
            AddToLookup(vector2Entries);
            AddToLookup(vector3Entries);
            AddToLookup(colorEntries);
        }

        private void AddToLookup<T>(List<T> list) where T : ReferenceEntryBase
        {
            foreach (var entry in list)
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
        public T Get<T>(string key) where T : ReferenceEntryBase
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
        public bool TryGet<T>(string key, out T value) where T : ReferenceEntryBase
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
