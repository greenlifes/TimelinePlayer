using UnityEngine;

namespace TimelineTool
{
    public enum ReferenceType
    {
        GameObject,
        MonoBehaviour,
        Int,
        Float,
        Bool,
        String,
        Vector2,
        Vector3,
        Color,
    }

    // -----------------------------------------------------------------------
    // Non-generic base — required so ReferenceHub can hold a List of mixed types.
    // Unity's [SerializeReference] resolves the concrete type at runtime.
    // -----------------------------------------------------------------------
    [System.Serializable]
    public abstract class ReferenceEntryBase
    {
        public string        Key;
        public ReferenceType Type;
    }

    // -----------------------------------------------------------------------
    // Generic middle layer — handles GetAs logic for all derived types.
    // -----------------------------------------------------------------------
    [System.Serializable]
    public abstract class ReferenceEntry<T> : ReferenceEntryBase
    {
        public abstract T Value { get; }
    }

    // -----------------------------------------------------------------------
    // Concrete entry types
    // -----------------------------------------------------------------------

    [System.Serializable]
    public class GameObjectEntry : ReferenceEntry<GameObject>
    {
        [SerializeField] private GameObject _value;
        public override GameObject Value => _value;
    }

    [System.Serializable]
    public class MonoBehaviourEntry : ReferenceEntry<MonoBehaviour>
    {
        [SerializeField] private MonoBehaviour _value;
        public override MonoBehaviour Value => _value;

        /// <summary>Convenience cast to a specific MonoBehaviour subtype.</summary>
        public T Get<T>() where T : MonoBehaviour => _value as T;
    }

    [System.Serializable]
    public class IntEntry : ReferenceEntry<int>
    {
        [SerializeField] private int _value;
        public override int Value => _value;
    }

    [System.Serializable]
    public class FloatEntry : ReferenceEntry<float>
    {
        [SerializeField] private float _value;
        public override float Value => _value;
    }

    [System.Serializable]
    public class BoolEntry : ReferenceEntry<bool>
    {
        [SerializeField] private bool _value;
        public override bool Value => _value;
    }

    [System.Serializable]
    public class StringEntry : ReferenceEntry<string>
    {
        [SerializeField] private string _value;
        public override string Value => _value ?? string.Empty;
    }

    [System.Serializable]
    public class Vector2Entry : ReferenceEntry<Vector2>
    {
        [SerializeField] private Vector2 _value;
        public override Vector2 Value => _value;
    }

    [System.Serializable]
    public class Vector3Entry : ReferenceEntry<Vector3>
    {
        [SerializeField] private Vector3 _value;
        public override Vector3 Value => _value;
    }

    [System.Serializable]
    public class ColorEntry : ReferenceEntry<Color>
    {
        [SerializeField] private Color _value = Color.white;
        public override Color Value => _value;
    }
}
