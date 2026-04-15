using UnityEngine;

namespace TimelineTool
{
    public enum ReferenceType
    {
        GameObject,
        Transform,
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
    // Non-generic base — required so ReferenceHub can hold a lookup of mixed types.
    // Each concrete type is stored in its own typed list in ReferenceHub.
    // -----------------------------------------------------------------------
    [System.Serializable]
    public abstract class ReferenceEntryBase
    {
        public string Key;

        // Maps ReferenceType → serialized field name + display label
        public static readonly (ReferenceType type, string propName, string label)[] TypeMeta =
        {
            (ReferenceType.GameObject,    "gameObjectEntries",    "GameObject"),
            (ReferenceType.Transform,     "transformEntries",     "Transform"),
            (ReferenceType.MonoBehaviour, "monoBehaviourEntries", "MonoBehaviour"),
            (ReferenceType.Int,           "intEntries",           "Int"),
            (ReferenceType.Float,         "floatEntries",         "Float"),
            (ReferenceType.Bool,          "boolEntries",          "Bool"),
            (ReferenceType.String,        "stringEntries",        "String"),
            (ReferenceType.Vector2,       "vector2Entries",       "Vector2"),
            (ReferenceType.Vector3,       "vector3Entries",       "Vector3"),
            (ReferenceType.Color,         "colorEntries",         "Color"),
        };
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
    public class TransformEntry : ReferenceEntry<Transform>
    {
        [SerializeField] private Transform _value;
        public override Transform Value => _value;
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
