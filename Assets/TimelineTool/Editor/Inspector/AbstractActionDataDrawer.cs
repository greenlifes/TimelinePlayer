using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TimelineTool.Editor
{
    /// <summary>
    /// PropertyDrawer for [SerializeReference] AbstractActionData fields.
    ///
    /// Header row: [▼ label] [ConcreteType ▾] [✕]
    ///   - Type dropdown lists every non-abstract AbstractActionData subclass found at compile time.
    ///   - Selecting a type creates a new inline instance (old data is discarded).
    ///   - ✕ clears the reference.
    ///
    /// Expanded block: draws each serialized field of the concrete instance inline.
    /// </summary>
    [CustomPropertyDrawer(typeof(AbstractActionData), useForChildren: true)]
    public class AbstractActionDataDrawer : PropertyDrawer
    {
        private static List<Type>   _types;
        private static string[]     _typeNames;   // index 0 = "(None)"
        private static GUIContent[] _typeLabels;

        private const float IndentOfs  = 10f;
        private const float ClearBtnW  = 22f;
        private const float TopPad     = 2f;
        private const float BottomPad  = 4f;

        // -----------------------------------------------------------------------

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnsureTypes();
            float h = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.managedReferenceValue != null && property.isExpanded)
            {
                h += TopPad;
                foreach (float ph in ChildHeights(property))
                    h += ph + EditorGUIUtility.standardVerticalSpacing;
                h += BottomPad;
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureTypes();
            EditorGUI.BeginProperty(position, label, property);

            float lineH   = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float y       = position.y;
            bool  hasVal  = property.managedReferenceValue != null;

            // ---- Header row ------------------------------------------------
            float labelW = EditorGUIUtility.labelWidth;
            float clearW = hasVal ? ClearBtnW : 0f;
            float dropW  = position.width - labelW - clearW;

            var labelRect = new Rect(position.x,             y, labelW,          lineH);
            var dropRect  = new Rect(position.x + labelW,    y, dropW  - 2f,     lineH);
            var clearRect = new Rect(position.x + position.width - ClearBtnW, y, ClearBtnW, lineH);

            // Foldout on label (only meaningful when a value exists)
            if (hasVal)
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, toggleOnLabelClick: true);
            else
                EditorGUI.LabelField(labelRect, label);

            // Type dropdown
            var curType = property.managedReferenceValue?.GetType();
            int curIdx  = curType != null ? _types.IndexOf(curType) + 1 : 0;

            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUI.Popup(dropRect, curIdx, _typeLabels);
            if (EditorGUI.EndChangeCheck())
            {
                property.managedReferenceValue = newIdx == 0
                    ? null
                    : Activator.CreateInstance(_types[newIdx - 1]);

                property.isExpanded = newIdx != 0;
                property.serializedObject.ApplyModifiedProperties();
            }

            // Clear button
            if (hasVal && GUI.Button(clearRect, "✕", EditorStyles.miniButton))
            {
                property.managedReferenceValue = null;
                property.isExpanded = false;
                property.serializedObject.ApplyModifiedProperties();
            }

            y += lineH + spacing;

            // ---- Inline block ----------------------------------------------
            if (!hasVal || !property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float blockH = GetPropertyHeight(property, label) - lineH - spacing;
            EditorGUI.DrawRect(
                new Rect(position.x + IndentOfs, y - 1f, position.width - IndentOfs, blockH),
                new Color(0f, 0f, 0f, 0.07f));

            y += TopPad;

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = prevIndent + 1;

            var copy = property.Copy();
            var end  = property.GetEndProperty(true);
            if (copy.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(copy, end)) break;
                    float ph = EditorGUI.GetPropertyHeight(copy, true);
                    EditorGUI.PropertyField(
                        new Rect(position.x + IndentOfs, y, position.width - IndentOfs, ph),
                        copy, includeChildren: true);
                    y += ph + spacing;
                }
                while (copy.NextVisible(false));
            }

            EditorGUI.indentLevel = prevIndent;
            EditorGUI.EndProperty();
        }

        // -----------------------------------------------------------------------

        private static IEnumerable<float> ChildHeights(SerializedProperty property)
        {
            var copy = property.Copy();
            var end  = property.GetEndProperty(true);
            if (!copy.NextVisible(true)) yield break;
            do
            {
                if (SerializedProperty.EqualContents(copy, end)) yield break;
                yield return EditorGUI.GetPropertyHeight(copy, true);
            }
            while (copy.NextVisible(false));
        }

        private static void EnsureTypes()
        {
            if (_types != null) return;

            _types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => !t.IsAbstract && !t.IsGenericType
                         && typeof(AbstractActionData).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();

            _typeNames  = new string[_types.Count + 1];
            _typeLabels = new GUIContent[_types.Count + 1];
            _typeNames[0]  = "(None)";
            _typeLabels[0] = new GUIContent("(None)");
            for (int i = 0; i < _types.Count; i++)
            {
                _typeNames[i + 1]  = _types[i].Name;
                _typeLabels[i + 1] = new GUIContent(_types[i].Name, _types[i].FullName);
            }
        }
    }
}
