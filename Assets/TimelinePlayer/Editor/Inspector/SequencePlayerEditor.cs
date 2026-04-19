using System.Collections.Generic;
using TimelinePlayer.Player;
using UnityEditor;
using UnityEngine;

namespace TimelinePlayer.Editor
{
    [CustomEditor(typeof(SequencePlayer))]
    public class SequencePlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var player = (SequencePlayer)target;

            EditorGUILayout.Space(8);

            // ---- Button 1: Extract track names as override bindings ------------
            if (GUILayout.Button("Extract Track Names from SequenceData", GUILayout.Height(28)))
                ExtractTrackNames(player);

            EditorGUILayout.Space(4);

            // ---- Button 2 & 3: Play / Stop (runtime only) ----------------------
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();

                var playStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                var stopStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                playStyle.normal.textColor = Application.isPlaying ? new Color(0.2f, 0.8f, 0.2f) : Color.gray;
                stopStyle.normal.textColor = Application.isPlaying ? new Color(0.9f, 0.3f, 0.3f) : Color.gray;

                if (GUILayout.Button("▶  PLAY", playStyle, GUILayout.Height(32)))
                    player.Play();

                if (GUILayout.Button("■  STOP", stopStyle, GUILayout.Height(32)))
                    player.Stop();

                EditorGUILayout.EndHorizontal();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("▶ PLAY  /  ■ STOP  are available in Play Mode.", MessageType.None);
            }
            else if (player.IsPlaying)
            {
                EditorGUILayout.HelpBox("Playing...", MessageType.None);
            }
        }

        // ------------------------------------------------------------------------

        private void ExtractTrackNames(SequencePlayer player)
        {
            var so = new SerializedObject(player);
            var sequenceDataProp = so.FindProperty("sequenceData");
            var bindingsProp = so.FindProperty("bindings");

            if (sequenceDataProp.objectReferenceValue is not TimelineSequenceData sequenceData)
            {
                EditorUtility.DisplayDialog("TimelineTool", "Assign a Sequence Data asset first.", "OK");
                return;
            }

            // Collect existing names so we don't create duplicates
            var existingNames = new HashSet<string>();
            for (int i = 0; i < bindingsProp.arraySize; i++)
            {
                string name = bindingsProp
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative("TrackName")
                    .stringValue;
                existingNames.Add(name);
            }

            int added = 0;
            foreach (var track in sequenceData.Tracks)
            {
                if (existingNames.Contains(track.TrackName)) continue;

                bindingsProp.arraySize++;
                var elem = bindingsProp.GetArrayElementAtIndex(bindingsProp.arraySize - 1);
                elem.FindPropertyRelative("TrackName").stringValue = track.TrackName;
                elem.FindPropertyRelative("OverrideHub").objectReferenceValue = null;
                added++;
            }

            so.ApplyModifiedProperties();

            Debug.Log(added > 0
                ? $"[TimelineTool] Added {added} override binding(s) to SequencePlayer."
                : "[TimelineTool] All track names already present.");
        }
    }
}
