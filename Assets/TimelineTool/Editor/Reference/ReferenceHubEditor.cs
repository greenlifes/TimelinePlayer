using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TimelineTool.Editor
{
    [CustomEditor(typeof(ReferenceHub))]
    public class ReferenceHubEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, SerializedProperty> _props = new();
        private readonly Dictionary<string, ReorderableList>    _lists = new();
        private ReferenceType _addType = ReferenceType.GameObject;

        private const float RowH     = 20f;
        private const float RowPad   = 4f;
        private const float KeyRatio = 0.40f;

        // -----------------------------------------------------------------------

        private void OnEnable()
        {
            foreach (var (_, propName, label) in ReferenceEntryBase.TypeMeta)
            {
                var prop = serializedObject.FindProperty(propName);
                if (prop == null) continue;   // field missing or compile error — skip safely
                _props[propName] = prop;
                _lists[propName] = BuildList(prop, label);
            }
        }

        private ReorderableList BuildList(SerializedProperty listProp, string label)
        {
            var list = new ReorderableList(
                serializedObject, listProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            list.drawHeaderCallback = rect =>
            {
                float w    = rect.width;
                float keyX = w * 0.30f;
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, keyX, rect.height),
                    label, EditorStyles.boldLabel);
                EditorGUI.LabelField(
                    new Rect(rect.x + keyX, rect.y, w * KeyRatio, rect.height),
                    "Key", EditorStyles.miniLabel);
                EditorGUI.LabelField(
                    new Rect(rect.x + keyX + w * KeyRatio, rect.y, w - keyX - w * KeyRatio, rect.height),
                    "Value", EditorStyles.miniLabel);
            };

            list.elementHeightCallback = _ => RowH + RowPad;

            list.drawElementCallback = (rect, index, _, _) =>
                DrawRow(listProp, rect, index);

            list.onAddCallback = l =>
            {
                l.serializedProperty.arraySize++;
                var elem = l.serializedProperty.GetArrayElementAtIndex(l.serializedProperty.arraySize - 1);
                elem.FindPropertyRelative("Key").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
            };

            return list;
        }

        // -----------------------------------------------------------------------

        private void DrawRow(SerializedProperty listProp, Rect rect, int index)
        {
            var elem = listProp.GetArrayElementAtIndex(index);
            rect.y      += RowPad * 0.5f;
            rect.height  = RowH;

            float keyW = rect.width * KeyRatio;
            float valW = rect.width - keyW - 6f;

            var keyRect = new Rect(rect.x,        rect.y, keyW - 3f, rect.height);
            var valRect = new Rect(rect.x + keyW, rect.y, valW,      rect.height);

            var keyProp = elem.FindPropertyRelative("Key");

            bool isDupe   = IsDuplicateKey(listProp, index, keyProp?.stringValue);
            var prevColor = GUI.color;
            if (isDupe) GUI.color = new Color(1f, 0.45f, 0.45f);
            EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
            GUI.color = prevColor;

            var valueProp = elem.FindPropertyRelative("_value");
            if (valueProp == null) return;

            if (valueProp.propertyType == SerializedPropertyType.Vector4)
            {
                EditorGUI.BeginChangeCheck();
                var newVal = EditorGUI.Vector4Field(valRect, GUIContent.none, valueProp.vector4Value);
                if (EditorGUI.EndChangeCheck())
                    valueProp.vector4Value = newVal;
            }
            else
                EditorGUI.PropertyField(valRect, valueProp, GUIContent.none);
        }

        // -----------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool anyDupe = false;

            // Draw only non-empty lists
            foreach (var (_, propName, _) in ReferenceEntryBase.TypeMeta)
            {
                if (!_props.TryGetValue(propName, out var prop) || prop.arraySize == 0) continue;

                _lists[propName].DoLayoutList();
                EditorGUILayout.Space(2f);

                if (!anyDupe) anyDupe = HasAnyDuplicateKey(prop);
            }

            // ── Add Entry ─────────────────────────────────────────────────────
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            _addType = (ReferenceType)EditorGUILayout.EnumPopup(_addType, GUILayout.Width(140f));
            if (GUILayout.Button("Add Entry", GUILayout.Width(90f)))
                AddEntry(_addType);
            EditorGUILayout.EndHorizontal();

            if (anyDupe)
                EditorGUILayout.HelpBox(
                    "Duplicate keys detected — only the first entry will be used.",
                    MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        // -----------------------------------------------------------------------

        private void AddEntry(ReferenceType type)
        {
            foreach (var (t, propName, _) in ReferenceEntryBase.TypeMeta)
            {
                if (t != type) continue;
                if (!_props.TryGetValue(propName, out var prop)) return;
                prop.arraySize++;
                var elem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                elem.FindPropertyRelative("Key").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
                return;
            }
        }

        // Keys must be unique across ALL lists (the lookup is global)
        private bool IsDuplicateKey(SerializedProperty listProp, int index, string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            foreach (var (_, propName, _) in ReferenceEntryBase.TypeMeta)
            {
                if (!_props.TryGetValue(propName, out var prop)) continue;
                bool isSame = SerializedProperty.EqualContents(prop, listProp);
                for (int i = 0; i < prop.arraySize; i++)
                {
                    if (isSame && i == index) continue;
                    if (prop.GetArrayElementAtIndex(i).FindPropertyRelative("Key")?.stringValue == key)
                        return true;
                }
            }
            return false;
        }

        private bool HasAnyDuplicateKey(SerializedProperty listProp)
        {
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var k = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Key")?.stringValue;
                if (IsDuplicateKey(listProp, i, k)) return true;
            }
            return false;
        }
    }
}
