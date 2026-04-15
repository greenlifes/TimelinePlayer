using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TimelineTool.Editor
{
    [CustomEditor(typeof(ReferenceHub))]
    public class ReferenceHubEditor : UnityEditor.Editor
    {
        private ReorderableList    _list;
        private SerializedProperty _entriesProp;

        private const float RowH   = 20f;
        private const float RowPad = 4f;

        private const float KeyRatio  = 0.30f;
        private const float TypeRatio = 0.25f;

        // -----------------------------------------------------------------------

        private void OnEnable()
        {
            _entriesProp = serializedObject.FindProperty("entries");
            BuildList();
        }

        private void BuildList()
        {
            _list = new ReorderableList(
                serializedObject, _entriesProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _list.drawHeaderCallback = rect =>
            {
                float w = rect.width;
                EditorGUI.LabelField(new Rect(rect.x,              rect.y, w * KeyRatio,  rect.height), "Key",   EditorStyles.boldLabel);
                EditorGUI.LabelField(new Rect(rect.x + w * KeyRatio, rect.y, w * TypeRatio, rect.height), "Type",  EditorStyles.boldLabel);
                EditorGUI.LabelField(new Rect(rect.x + w * (KeyRatio + TypeRatio), rect.y, w * (1 - KeyRatio - TypeRatio), rect.height), "Value", EditorStyles.boldLabel);
            };

            _list.elementHeightCallback = _ => RowH + RowPad;

            _list.drawElementCallback = DrawRow;

            _list.onAddCallback = _ =>
            {
                _entriesProp.arraySize++;
                var prop = _entriesProp.GetArrayElementAtIndex(_entriesProp.arraySize - 1);
                prop.managedReferenceValue = CreateEntry(ReferenceType.GameObject);
                serializedObject.ApplyModifiedProperties();
            };
        }

        // -----------------------------------------------------------------------

        private void DrawRow(Rect rect, int index, bool isActive, bool isFocused)
        {
            var prop  = _entriesProp.GetArrayElementAtIndex(index);
            var entry = prop.managedReferenceValue as ReferenceEntryBase;
            if (entry == null) return;

            rect.y     += RowPad * 0.5f;
            rect.height = RowH;

            float w    = rect.width;
            float keyW = w * KeyRatio;
            float typW = w * TypeRatio;
            float valW = w - keyW - typW - 6f;

            var keyRect  = new Rect(rect.x,            rect.y, keyW - 3, rect.height);
            var typeRect = new Rect(rect.x + keyW,     rect.y, typW - 3, rect.height);
            var valRect  = new Rect(rect.x + keyW + typW, rect.y, valW,  rect.height);

            // -- Key (highlight duplicate in red) ----------------------------
            bool isDupe   = IsDuplicateKey(index, entry.Key);
            var prevColor = GUI.color;
            if (isDupe) GUI.color = new Color(1f, 0.45f, 0.45f);
            EditorGUI.PropertyField(keyRect, prop.FindPropertyRelative("Key"), GUIContent.none);
            GUI.color = prevColor;

            // -- Type popup (switching replaces the managed reference) -------
            var curType = ToReferenceType(entry);
            EditorGUI.BeginChangeCheck();
            var newType = (ReferenceType)EditorGUI.EnumPopup(typeRect, curType);
            if (EditorGUI.EndChangeCheck() && newType != curType)
            {
                var replacement = CreateEntry(newType);
                replacement.Key = entry.Key;          // preserve key
                prop.managedReferenceValue = replacement;
                serializedObject.ApplyModifiedProperties();
                return;                               // value field belongs to new instance
            }

            // -- Value field -------------------------------------------------
            var valueProp = prop.FindPropertyRelative("_value");
            if (valueProp == null) return;

            DrawValueField(entry, valueProp, valRect);
        }

        private static void DrawValueField(
            ReferenceEntryBase entry, SerializedProperty valueProp, Rect rect)
        {
            switch (entry)
            {
                case GameObjectEntry:
                    EditorGUI.ObjectField(rect, valueProp, typeof(GameObject), GUIContent.none);
                    break;

                case MonoBehaviourEntry:
                    EditorGUI.ObjectField(rect, valueProp, typeof(MonoBehaviour), GUIContent.none);
                    break;

                case IntEntry:
                case FloatEntry:
                case StringEntry:
                case Vector2Entry:
                case Vector3Entry:
                    EditorGUI.PropertyField(rect, valueProp, GUIContent.none);
                    break;

                case BoolEntry:
                    var toggle = new Rect(rect.x + rect.width * 0.5f - 8f, rect.y, 16f, rect.height);
                    valueProp.boolValue = EditorGUI.Toggle(toggle, valueProp.boolValue);
                    break;

                case ColorEntry:
                    EditorGUI.PropertyField(rect, valueProp, GUIContent.none);
                    break;
            }
        }

        // -----------------------------------------------------------------------
        // Helpers

        private static ReferenceEntryBase CreateEntry(ReferenceType type) => type switch
        {
            ReferenceType.GameObject    => new GameObjectEntry    { Type = ReferenceType.GameObject    },
            ReferenceType.MonoBehaviour => new MonoBehaviourEntry { Type = ReferenceType.MonoBehaviour },
            ReferenceType.Int           => new IntEntry           { Type = ReferenceType.Int           },
            ReferenceType.Float         => new FloatEntry         { Type = ReferenceType.Float         },
            ReferenceType.Bool          => new BoolEntry          { Type = ReferenceType.Bool          },
            ReferenceType.String        => new StringEntry        { Type = ReferenceType.String        },
            ReferenceType.Vector2       => new Vector2Entry       { Type = ReferenceType.Vector2       },
            ReferenceType.Vector3       => new Vector3Entry       { Type = ReferenceType.Vector3       },
            ReferenceType.Color         => new ColorEntry         { Type = ReferenceType.Color         },
            _                           => new GameObjectEntry    { Type = ReferenceType.GameObject    },
        };

        private static ReferenceType ToReferenceType(ReferenceEntryBase entry) => entry switch
        {
            GameObjectEntry    => ReferenceType.GameObject,
            MonoBehaviourEntry => ReferenceType.MonoBehaviour,
            IntEntry           => ReferenceType.Int,
            FloatEntry         => ReferenceType.Float,
            BoolEntry          => ReferenceType.Bool,
            StringEntry        => ReferenceType.String,
            Vector2Entry       => ReferenceType.Vector2,
            Vector3Entry       => ReferenceType.Vector3,
            ColorEntry         => ReferenceType.Color,
            _                  => ReferenceType.GameObject,
        };

        private bool IsDuplicateKey(int index, string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            for (int i = 0; i < _entriesProp.arraySize; i++)
            {
                if (i == index) continue;
                var e = _entriesProp.GetArrayElementAtIndex(i).managedReferenceValue as ReferenceEntryBase;
                if (e?.Key == key) return true;
            }
            return false;
        }

        private bool HasAnyDuplicateKey()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _entriesProp.arraySize; i++)
            {
                var e = _entriesProp.GetArrayElementAtIndex(i).managedReferenceValue as ReferenceEntryBase;
                if (e != null && !string.IsNullOrEmpty(e.Key) && !seen.Add(e.Key))
                    return true;
            }
            return false;
        }

        // -----------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _list.DoLayoutList();
            if (HasAnyDuplicateKey())
                EditorGUILayout.HelpBox(
                    "Duplicate keys detected — only the first entry will be used.",
                    MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
